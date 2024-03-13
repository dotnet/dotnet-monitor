// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    // Creates a scope in which parameter capturing is disabled.
    internal sealed class NoProbeContext : IDisposable
    {
        public NoProbeContext()
        {
            FunctionProbesStub.PauseProbing();
        }

        public void Dispose()
        {
            FunctionProbesStub.ResumeProbing();
        }
    }
}
