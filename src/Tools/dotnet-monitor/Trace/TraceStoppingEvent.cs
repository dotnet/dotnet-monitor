// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal readonly struct TraceStoppingEvent
    {
        public TraceStoppingEvent(string providerName, string eventName, IDictionary<string, string> payloadFilter)
        {
            ProviderName = providerName;
            EventName = eventName;
            PayloadFilter = payloadFilter;
        }

        public string ProviderName { get; }
        public string EventName { get; }
        public IDictionary<string, string> PayloadFilter { get; }
    }
}
