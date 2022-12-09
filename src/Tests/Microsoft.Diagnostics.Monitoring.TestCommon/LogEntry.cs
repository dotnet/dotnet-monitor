// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class LogEntry
    {
        public string Category { get; set; }

        public int EventId { get; set; }

        public string EventName { get; set; }

        public string Exception { get; set; }

        public LogLevel LogLevel { get; set; }

        public string Message { get; set; }

        public Dictionary<string, JsonElement> Scopes { get; set; }

        public Dictionary<string, string> State { get; set; }

        public string Timestamp { get; set; }
    }
}
