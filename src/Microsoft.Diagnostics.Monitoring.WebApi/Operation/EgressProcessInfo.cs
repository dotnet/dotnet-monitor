// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class EgressProcessInfo
    {
        public string ProcessName { get; }
        public int ProcessId { get; }
        public Guid RuntimeInstanceCookie { get; }

        public EgressProcessInfo(string processName, int processId, Guid runtimeInstanceCookie)
        {
            this.ProcessName = processName;
            this.ProcessId = processId;
            this.RuntimeInstanceCookie = runtimeInstanceCookie;
        }
    }
}
