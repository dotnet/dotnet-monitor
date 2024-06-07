// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal interface IMethodDescriptionValidator
    {
        public bool IsMethodDescriptionAllowed(MethodDescription methodDescription);
    }
}
