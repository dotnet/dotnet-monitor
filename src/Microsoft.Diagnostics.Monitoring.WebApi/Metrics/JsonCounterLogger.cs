// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class JsonCounterLogger : StreamingCounterLogger
    {
        public JsonCounterLogger(Stream stream) : base(stream)
        {
        }

        protected override void SerializeCounter(Stream stream, List<ICounterPayload> counter)
        {
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

                int index = 0; // temporary only

                foreach (var individualCounter in counter)
                {
                    //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
                    writer.WriteNumber("value " + index, double.IsNaN(individualCounter.Value) ? 0.0 : individualCounter.Value);
                    index += 1;
                }

                writer.WriteEndObject();
            }
            stream.WriteByte((byte)'\n');
        }
    }
}
