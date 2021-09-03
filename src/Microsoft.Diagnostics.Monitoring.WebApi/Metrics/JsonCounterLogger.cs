// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class JsonCounterLogger : StreamingCounterLogger
    {
        public JsonCounterLogger(Stream stream) : base(stream)
        {
        }

        protected override void SerializeCounter(Stream stream, ICounterPayload counter)
        {
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

                //Some versions of .Net return invalid metric numbers. See https://github.com/dotnet/runtime/pull/46938
                writer.WriteNumber("value", double.IsNaN(counter.Value) ? 0.0 : counter.Value);
                writer.WriteEndObject();
            }
            stream.WriteByte((byte)'\n');
        }
    }
}
