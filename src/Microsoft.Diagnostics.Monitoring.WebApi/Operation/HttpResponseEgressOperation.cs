// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class HttpResponseEgressOperation : IEgressOperation
    {
        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get { return null; } }

        public HttpResponseEgressOperation(IProcessInfo processInfo)
        {
            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
        }
    }
}
