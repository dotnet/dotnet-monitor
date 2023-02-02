// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    // All log events have this structure (plus additional fields
    // not needed by the test runner for identifying events).
    public sealed class ConsoleLogEvent
    {
        public string Category { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public string Exception { get; set; }

        public Dictionary<string, JsonElement> State { get; set; }
    }
}
