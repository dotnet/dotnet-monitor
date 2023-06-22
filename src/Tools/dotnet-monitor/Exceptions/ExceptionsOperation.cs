// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperation : IArtifactOperation
    {
        private static byte[] JsonRecordDelimiter = new byte[] { (byte)'\n' };

        private static byte[] JsonSequenceRecordSeparator = new byte[] { 0x1E };

        private readonly ExceptionsFormat _format;
        private readonly IExceptionsStore _store;

        public ExceptionsOperation(IExceptionsStore store, ExceptionsFormat format)
        {
            _store = store;
            _format = format;
        }

        public string ContentType => _format switch
        {
            ExceptionsFormat.PlainText => ContentTypes.TextPlain,
            ExceptionsFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            ExceptionsFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };

        public bool IsStoppable => false;

        public async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            startCompletionSource?.TrySetResult(null);


            IEnumerable<IExceptionInstance> exceptions = _store.GetSnapshot();

            switch (_format)
            {
                case ExceptionsFormat.JsonSequence:
                case ExceptionsFormat.NewlineDelimitedJson:
                    await WriteJson(outputStream, exceptions, token);
                    break;
                case ExceptionsFormat.PlainText:
                    await WriteText(outputStream, exceptions, token);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public string GenerateFileName()
        {
            throw new NotSupportedException();
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        private async Task WriteJson(Stream stream, IEnumerable<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in instances)
            {
                await WriteJsonInstance(stream, instance, token);
            }
        }

        private async Task WriteJsonInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            if (_format == ExceptionsFormat.JsonSequence)
            {
                await stream.WriteAsync(JsonSequenceRecordSeparator, token);
            }

            // Make sure dotnet-monitor is self-consistent with other features that print type and stack information.
            // For example, the stacks and exceptions features should print structured stack traces exactly the same way.
            // CONSIDER: Investigate if other tools have "standard" formats for printing structured stacks and exceptions.
            await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();
                // Writes the timestamp in ISO 8601 format
                writer.WriteString("timestamp", instance.Timestamp);
                writer.WriteString("typeName", instance.TypeName);
                writer.WriteString("moduleName", instance.ModuleName);
                writer.WriteString("message", instance.Message);

                writer.WriteStartObject("callStack");
                writer.WriteNumber("threadId", instance.CallStack.ThreadId);
                writer.WriteString("threadName", instance.CallStack.ThreadName);

                writer.WriteStartArray("frames");

                StringBuilder builder = new();
                foreach (var frame in instance.CallStack.Frames)
                {
                    writer.WriteStartObject();

                    AssembleMethodName(builder, frame.MethodName, frame.ParameterTypes, frame.TypeArgs);
                    writer.WriteString("methodName", builder.ToString());
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

        private static async Task WriteText(Stream stream, IEnumerable<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in instances)
            {
                await WriteTextInstance(stream, instance, token);
            }
        }

        private static async Task WriteTextInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            // This format is similar of that which is written to the console when an unhandled exception occurs. Each
            // exception will appear as:

            // First chance exception. <TypeName>: <Message>
            //   at Class.Method(ParameterType1, ParameterType2, ...)

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteLineAsync(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.OutputFormatString_FirstChanceException,
                    instance.TypeName,
                    instance.Message));

            if (instance.CallStack != null)
            {
                StringBuilder builder = new();
                foreach (CallStackFrame frame in instance.CallStack.Frames)
                {
                    AssembleMethodName(builder, frame.MethodName, frame.ParameterTypes, frame.TypeArgs);
                    await writer.WriteLineAsync(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.OutputFormatString_FirstChanceExceptionStackFrame,
                            frame.ClassName,
                            builder.ToString()));
                }
            }

            await writer.WriteLineAsync();

            await writer.FlushAsync();
        }

        private static void AssembleMethodName(StringBuilder builder, string methodName, string parameterTypes, string typeArgs)
        {
            builder.Clear();
            builder.Append(methodName);
            builder.Append(parameterTypes);
            builder.Append(typeArgs);
        }
    }
}
