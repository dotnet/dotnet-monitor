// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal interface IFunctionProbesManager : IDisposable
    {
        public Task StartCapturingAsync(IList<MethodInfo> methods, CancellationToken token);

        public Task StopCapturingAsync(CancellationToken token);

        public event EventHandler<InstrumentedMethod> OnProbeFault;
    }
}
