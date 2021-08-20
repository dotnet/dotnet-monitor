// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class EndpointInfoExtensions
    {
        public static string GetDebuggerDisplay(this IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"PID={endpointInfo.ProcessId}, Cookie={endpointInfo.RuntimeInstanceCookie}");
        }
    }
}
