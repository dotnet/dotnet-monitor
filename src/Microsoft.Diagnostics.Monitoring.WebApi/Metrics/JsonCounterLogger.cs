// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class JsonCounterLogger : StreamingCounterLogger
    {
        ILogger _logger;

        public JsonCounterLogger(Stream stream, ILogger logger) : base(stream)
        {
            _logger = logger;
        }

        protected override void SerializeCounter(Stream stream, List<ICounterPayload> counter)
        {
            if (counter[0] is ErrorPayload errorPayload)
            {
                _logger.LogWarning(errorPayload.ErrorMessage);

                return;
            }

            stream.WriteByte(StreamingLogger.JsonSequenceRecordSeparator);
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("timestamp", counter[0].Timestamp);
                writer.WriteString("provider", counter[0].Provider);
                writer.WriteString("name", counter[0].Name);
                writer.WriteString("displayName", counter[0].DisplayName);
                writer.WriteString("unit", counter[0].Unit);
                writer.WriteString("counterType", counter[0].CounterType.ToString());

                // Histogram - show quantiles
                if (counter.Count > 1)
                {
                    writer.WriteStartObject("value");
                    foreach (var individualCounter in counter)
                    {
                        string quantile = individualCounter.Metadata["quantile"]; // need to make this cleaner/safer
                        //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
                        writer.WriteNumber(quantile, double.IsNaN(individualCounter.Value) ? 0.0 : individualCounter.Value);
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
                    writer.WriteNumber("value", double.IsNaN(counter[0].Value) ? 0.0 : counter[0].Value);
                }

                writer.WriteEndObject();
            }
            stream.WriteByte((byte)'\n');
        }
    }
}
