// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
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
                Logger.ErrorPayload(errorPayload.ErrorMessage);
                return;
            }
            else if (counter is CounterEndedPayload)
            {
                Logger.CounterEndedPayload(counter.CounterMetadata.CounterName);
                return;
            }
            else if (!counter.EventType.IsValuePublishedEvent())
            {
                // Do we want to do anything with this payload?
                return;
            }

            if (counter is AggregatePercentilePayload aggregatePercentilePayload)
            {
                if (!aggregatePercentilePayload.Quantiles.Any())
                {
                    return;
                }
                await _stream.WriteAsync(JsonSequenceRecordSeparator);
                _bufferWriter.Clear();

                for (int i = 0; i < aggregatePercentilePayload.Quantiles.Length; i++)
                {
                    if (i > 0)
                    {
                        _bufferWriter.Write(JsonSequenceRecordSeparator.Span);
                    }
                    Quantile quantile = aggregatePercentilePayload.Quantiles[i];

                    SerializeCounterValues(counter.Timestamp,
                        counter.CounterMetadata.ProviderName,
                        counter.CounterMetadata.CounterName,
                        counter.DisplayName,
                        counter.Unit,
                        counter.CounterType.ToString(),
                        CounterUtilities.AppendPercentile(counter.ValueTags, quantile.Percentage),
                        quantile.Value,
                        counter.CounterMetadata.MeterTags,
                        counter.CounterMetadata.InstrumentTags);

                    if (i < aggregatePercentilePayload.Quantiles.Length - 1)
                    {
                        _bufferWriter.Write(NewLineSeparator.Span);
                    }
                }
            }
            else
            {
                await _stream.WriteAsync(JsonSequenceRecordSeparator);
                _bufferWriter.Clear();

                SerializeCounterValues(counter.Timestamp,
                    counter.CounterMetadata.ProviderName,
                    counter.CounterMetadata.CounterName,
                    counter.DisplayName,
                    counter.Unit,
                    counter.CounterType.ToString(),
                    counter.ValueTags,
                    counter.Value,
                    counter.CounterMetadata.MeterTags,
                    counter.CounterMetadata.InstrumentTags);
            }
            await _stream.WriteAsync(_bufferWriter.WrittenMemory);

            await _stream.WriteAsync(NewLineSeparator);
        }

        private void SerializeCounterValues(
            DateTime timestamp,
            string provider,
            string name,
            string displayName,
            string unit,
            string counterType,
            string tags,
            double value,
            string meterTags,
            string instrumentTags)
        {
            using var writer = new Utf8JsonWriter(_bufferWriter, new JsonWriterOptions { Indented = false });
            writer.WriteStartObject();
            writer.WriteString("timestamp", timestamp);
            writer.WriteString("provider", provider);
            writer.WriteString("name", name);
            writer.WriteString("displayName", displayName);
            writer.WriteString("unit", unit);
            writer.WriteString("counterType", counterType);

            writer.WriteString("tags", tags);

            //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
            writer.WriteNumber("value", double.IsNaN(value) ? 0.0 : value);

            writer.WriteString("meterTags", meterTags);
            writer.WriteString("instrumentTags", instrumentTags);

            writer.WriteEndObject();
        }
    }
}
