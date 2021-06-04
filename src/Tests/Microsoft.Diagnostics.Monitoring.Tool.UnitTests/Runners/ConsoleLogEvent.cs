// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    // All log events have this structure (plus additional fields
    // not needed by the test runner for identifying events).
    internal sealed  class ConsoleLogEvent
    {
        public string Category { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public Dictionary<string, JsonElement> State { get; set; }
    }
}
