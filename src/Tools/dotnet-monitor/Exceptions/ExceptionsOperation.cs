// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

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

            await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("typeName", instance.TypeName);
                writer.WriteString("moduleName", instance.ModuleName);
                writer.WriteString("message", instance.Message);
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

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteAsync("First chance exception. ");
            await writer.WriteAsync(instance.TypeName);
            await writer.WriteAsync(": ");
            await writer.WriteLineAsync(instance.Message);
            await writer.FlushAsync();
        }
    }
}
