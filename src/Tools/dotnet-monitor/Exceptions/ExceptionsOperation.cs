﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperation : IArtifactOperation
    {
        private static byte[] JsonRecordDelimiter = new byte[] { (byte)'\n' };

        private static byte[] JsonSequenceRecordSeparator = new byte[] { 0x1E };

        private const char GenericSeparator = ',';
        private const char MethodParameterTypesStart = '(';
        private const char MethodParameterTypesEnd = ')';

        private readonly ExceptionsConfigurationSettings _configuration;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ExceptionFormat _format;
        private readonly IExceptionsStore _store;

        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ExceptionsOperation(IEndpointInfo endpointInfo, IExceptionsStore store, ExceptionFormat format, ExceptionsConfigurationSettings configuration)
        {
            _endpointInfo = endpointInfo;
            _store = store;
            _format = format;
            _configuration = configuration;
        }

        public string ContentType => _format switch
        {
            ExceptionFormat.PlainText => ContentTypes.TextPlain,
            ExceptionFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            ExceptionFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };

        public bool IsStoppable => false;

        public Task Started => _startCompletionSource.Task;

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            _startCompletionSource.TrySetResult();

            IReadOnlyList<IExceptionInstance> exceptions = _store.GetSnapshot();

            switch (_format)
            {
                case ExceptionFormat.JsonSequence:
                case ExceptionFormat.NewlineDelimitedJson:
                    await WriteJson(outputStream, exceptions, token);
                    break;
                case ExceptionFormat.PlainText:
                    await WriteText(outputStream, exceptions, token);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public string GenerateFileName()
        {
            string extension = _format == ExceptionFormat.PlainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{_endpointInfo.ProcessId}.exceptions.{extension}");
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        private async Task WriteJson(Stream stream, IReadOnlyList<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in FilterExceptions(_configuration, instances))
            {
                await WriteJsonInstance(stream, instance, token);
            }
        }

        internal static List<IExceptionInstance> FilterExceptions(ExceptionsConfigurationSettings configuration, IReadOnlyList<IExceptionInstance> instances)
        {
            List<IExceptionInstance> filteredInstances = new List<IExceptionInstance>();
            foreach (IExceptionInstance instance in instances)
            {
                if (FilterException(configuration, instance))
                {
                    filteredInstances.Add(instance);
                }
            }

            return filteredInstances;
        }

        internal static bool FilterException(ExceptionsConfigurationSettings configuration, IExceptionInstance instance)
        {
            if (configuration.Exclude.Count > 0)
            {
                // filter out exceptions that match the filter
                if (configuration.ShouldExclude(instance))
                {
                    return false;
                }
            }

            if (configuration.Include.Count > 0)
            {
                // filter out exceptions that don't match the filter
                if (configuration.ShouldInclude(instance))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private async Task WriteJsonInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            if (_format == ExceptionFormat.JsonSequence)
            {
                await stream.WriteAsync(JsonSequenceRecordSeparator, token);
            }

            // Make sure dotnet-monitor is self-consistent with other features that print type and stack information.
            // For example, the stacks and exceptions features should print structured stack traces exactly the same way.
            // CONSIDER: Investigate if other tools have "standard" formats for printing structured stacks and exceptions.
            await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", instance.Id);
                // Writes the timestamp in ISO 8601 format
                writer.WriteString("timestamp", instance.Timestamp);
                writer.WriteString("typeName", instance.TypeName);
                writer.WriteString("moduleName", instance.ModuleName);
                writer.WriteString("message", instance.Message);

                if (IncludeActivityId(instance))
                {
                    writer.WriteStartObject("activity");
                    writer.WriteString("id", instance.ActivityId);
                    writer.WriteString("idFormat", instance.ActivityIdFormat.ToString("G"));
                    writer.WriteEndObject();
                }

                writer.WriteStartArray("innerExceptions");
                foreach (ulong innerExceptionId in instance.InnerExceptionIds)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("id", innerExceptionId);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteStartObject("callStack");
                writer.WriteNumber("threadId", instance.CallStack.ThreadId);
                writer.WriteString("threadName", instance.CallStack.ThreadName);

                writer.WriteStartArray("frames");

                foreach (var frame in instance.CallStack.Frames)
                {
                    writer.WriteStartObject();

                    writer.WriteString("methodName", frame.MethodName);
                    writer.WriteStartArray("parameterTypes");
                    foreach (string parameterType in frame.ParameterTypes)
                    {
                        writer.WriteStringValue(parameterType);
                    }
                    writer.WriteEndArray(); // end parameterTypes
                    writer.WriteString("className", frame.ClassName);
                    writer.WriteString("moduleName", frame.ModuleName);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray(); // end frames
                writer.WriteEndObject(); // end callStack
                writer.WriteEndObject(); // end.
            }

            await stream.WriteAsync(JsonRecordDelimiter, token);
        }

        private async Task WriteText(Stream stream, IReadOnlyList<IExceptionInstance> instances, CancellationToken token)
        {
            var filteredInstances = FilterExceptions(_configuration, instances);

            Dictionary<ulong, IExceptionInstance> priorInstances = new(filteredInstances.Count);

            foreach (IExceptionInstance currentInstance in filteredInstances)
            {
                // Skip writing the exception if it does not have a call stack, which
                // indicates that the exception was not thrown. It is likely to be referenced
                // as an inner exception of a thrown exception.
                if (currentInstance.CallStack?.Frames.Count != 0)
                {
                    await WriteTextInstance(stream, currentInstance, priorInstances, token);
                }
                priorInstances.Add(currentInstance.Id, currentInstance);
            }
        }

        private static async Task WriteTextInstance(
            Stream stream,
            IExceptionInstance currentInstance,
            IDictionary<ulong, IExceptionInstance> priorInstances,
            CancellationToken token)
        {
            // This format is similar of that which is written to the console when an unhandled exception occurs. Each
            // exception will appear as:

            // First chance exception at <TimeStamp>
            // <TypeName>: <Message>
            //  ---> <InnerExceptionTypeName>: <InnerExceptionMessage>
            //   at <StackFrameClass>.<StackFrameMethod>(<ParameterType1>, <ParameterType2>, ...)
            //   --- End of inner exception stack trace ---
            //   at <StackFrameClass>.<StackFrameMethod>(<ParameterType1>, <ParameterType2>, ...)

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteAsync("First chance exception at ");
            await writer.WriteAsync(currentInstance.Timestamp.ToString("O", CultureInfo.InvariantCulture));

            await writer.WriteLineAsync();
            await WriteTextExceptionFormat(writer, currentInstance);
            await WriteTextInnerExceptionsAndStackFrames(writer, currentInstance, priorInstances);

            if (IncludeActivityId(currentInstance))
            {
                // ActivityIdFormat is intentionally being omitted
                await writer.WriteLineAsync();
                await writer.WriteAsync($"Activity ID: {currentInstance.ActivityId}");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

#if NET8_0_OR_GREATER
            await writer.FlushAsync(token);
#else
            await writer.FlushAsync();
#endif
        }

        // Writes the inner exceptions and stack frames of the current exception:
        // - The primary inner exception is written with a separator message.
        // - The call stack frames are written for the current exception
        // - The remaining inner exceptions are written with their inner exception index
        // The above fits the format of inner exception and call stack information reported
        // by AggregateException instances.
        private static async Task WriteTextInnerExceptionsAndStackFrames(TextWriter writer, IExceptionInstance currentInstance, IDictionary<ulong, IExceptionInstance> priorInstances)
        {
            if (currentInstance.InnerExceptionIds?.Length > 0)
            {
                if (priorInstances.TryGetValue(
                        currentInstance.InnerExceptionIds[0],
                        out IExceptionInstance primaryInnerInstance))
                {
                    await WriteTextInnerException(writer, primaryInnerInstance, 0, priorInstances);

                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   --- End of inner exception stack trace ---");
                }
                else
                {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   --- The inner exception was not included in the filter ---");
                }
            }

            if (null != currentInstance.CallStack)
            {
                foreach (CallStackFrame frame in currentInstance.CallStack.Frames)
                {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   at ");
                    await writer.WriteAsync(frame.ClassName);
                    await writer.WriteAsync(".");
                    await writer.WriteAsync(frame.MethodName);
                    await writer.WriteAsync(MethodParameterTypesStart);

                    for (int i = 0; i < frame.ParameterTypes.Count; i++)
                    {
                        await writer.WriteAsync(frame.ParameterTypes[i]);

                        if (i < frame.ParameterTypes.Count - 1)
                        {
                            await writer.WriteAsync(GenericSeparator);
                        }
                    }
                    await writer.WriteAsync(MethodParameterTypesEnd);
                }
            }

            if (currentInstance.InnerExceptionIds?.Length > 1)
            {
                for (int index = 1; index < currentInstance.InnerExceptionIds.Length; index++)
                {
                    if (priorInstances.TryGetValue(
                        currentInstance.InnerExceptionIds[index],
                        out IExceptionInstance secondaryInnerInstance))
                    {
                        await WriteTextInnerException(writer, secondaryInnerInstance, index, priorInstances);
                    }
                }
            }
        }

        // Writes the specified exception as an inner exception with the appropriate delimiters.
        private static async Task WriteTextInnerException(TextWriter writer, IExceptionInstance currentInstance, int currentIndex, IDictionary<ulong, IExceptionInstance> priorInstances)
        {
            await writer.WriteLineAsync();
            await writer.WriteAsync(" ---> ");

            if (0 < currentIndex)
            {
                await writer.WriteAsync("(Inner Exception #");
                await writer.WriteAsync(currentIndex.ToString("D", CultureInfo.InvariantCulture));
                await writer.WriteAsync(") ");
            }

            await WriteTextExceptionFormat(writer, currentInstance);

            await WriteTextInnerExceptionsAndStackFrames(writer, currentInstance, priorInstances);

            if (0 < currentIndex)
            {
                await writer.WriteAsync("<---");
            }
        }

        // Writes the basic exception information, namely the type and message
        private static async Task WriteTextExceptionFormat(TextWriter writer, IExceptionInstance instance)
        {
            await writer.WriteAsync(instance.TypeName);
            if (!string.IsNullOrEmpty(instance.Message))
            {
                await writer.WriteAsync(": ");
                await writer.WriteAsync(instance.Message);
            }
        }

        private static bool IncludeActivityId(IExceptionInstance instance)
        {
            return !string.IsNullOrEmpty(instance.ActivityId);
        }
    }
}
