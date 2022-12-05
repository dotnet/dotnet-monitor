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

        protected override void SerializeCounter(Stream stream, ICounterPayload counter)
        {
            if (counter is ErrorPayload errorPayload)
            {
                _logger.LogWarning(errorPayload.ErrorMessage);

                return;
            }

            stream.WriteByte(StreamingLogger.JsonSequenceRecordSeparator);
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
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
            stream.WriteByte((byte)'\n');
        }
    }
}
