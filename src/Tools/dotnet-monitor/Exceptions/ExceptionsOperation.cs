// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

                MemoryStream outputStream = new();
                StacksFormatter formatter = StackUtilities.CreateFormatter(StackFormat.Json, outputStream);
                await formatter.FormatStack(instance.CallStackResult, token);
                outputStream.Position = 0;

                writer.WritePropertyName("callStack");
                writer.WriteRawValue(new StreamReader(outputStream).ReadToEnd());

                writer.WriteEndObject();
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
            //   at Class.Method

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteLineAsync(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.OutputFormatString_FirstChanceException,
                    instance.TypeName,
                    instance.Message));

            if (instance.CallStackResult.Stacks.Any())
            {
                CallStack stack = instance.CallStackResult.Stacks.First(); // We know the result only has a single stack

                foreach (CallStackFrame frame in stack.Frames)
                {
                    Monitoring.WebApi.Models.CallStackFrame frameModel = StacksFormatter.CreateFrameModel(frame, instance.CallStackResult.NameCache);

                    await writer.WriteLineAsync(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.OutputFormatString_FirstChanceExceptionStackFrame,
                            frameModel.ClassName,
                            frameModel.MethodName));
                }
            }

            await writer.FlushAsync();
        }
    }
}
