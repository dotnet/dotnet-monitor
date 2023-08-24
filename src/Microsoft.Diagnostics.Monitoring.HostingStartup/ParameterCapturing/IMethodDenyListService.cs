// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal interface IMethodDenyListService
    {
        public void ValidateMethods(IEnumerable<MethodDescription> methods);
    }
}
