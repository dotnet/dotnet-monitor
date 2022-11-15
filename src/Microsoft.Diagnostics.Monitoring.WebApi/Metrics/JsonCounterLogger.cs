// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class JsonCounterLogger : StreamingCounterLogger
    {
        // The default metrics providers will produce about 470 bytes of data
        // at most per counter value. Set the buffer to be slightly larger to
        // avoid reallocation.
        private const int InitialBufferCapacity = 500;

        private static readonly ReadOnlyMemory<byte> JsonSequenceRecordSeparator =
            new byte[] { StreamingLogger.JsonSequenceRecordSeparator };
        private static readonly ReadOnlyMemory<byte> NewLineSeparator =
            new byte[] { (byte)'\n' };

        private readonly ArrayBufferWriter<byte> _bufferWriter;
        private readonly Stream _stream;

        public JsonCounterLogger(Stream stream, ILogger logger)
            : base(logger)
        {
            _bufferWriter = new(InitialBufferCapacity);
            _stream = stream;
        }

        protected override async Task SerializeAsync(ICounterPayload counter)
        {
            if (counter is ErrorPayload errorPayload)
            {
                Logger.LogWarning(errorPayload.ErrorMessage);

                return;
            }

            await _stream.WriteAsync(JsonSequenceRecordSeparator);
            _bufferWriter.Clear();
            using (var writer = new Utf8JsonWriter(_bufferWriter, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("timestamp", counter.Timestamp);
                writer.WriteString("provider", counter.Provider);
                writer.WriteString("name", counter.Name);
                writer.WriteString("displayName", counter.DisplayName);
                writer.WriteString("unit", counter.Unit);
                writer.WriteString("counterType", counter.CounterType.ToString());

                string tagsVal = string.Empty;

                if (counter.Metadata.TryGetValue("quantile", out string percentile))
                {
                    tagsVal = "Percentile=" + percentile; // note that this is currently decimal not percentile
                }

                writer.WriteString("tags", tagsVal);

                //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
                writer.WriteNumber("value", double.IsNaN(counter.Value) ? 0.0 : counter.Value);

                writer.WriteEndObject();
            }
            await _stream.WriteAsync(_bufferWriter.WrittenMemory);

            await _stream.WriteAsync(NewLineSeparator);
        }
    }
}
