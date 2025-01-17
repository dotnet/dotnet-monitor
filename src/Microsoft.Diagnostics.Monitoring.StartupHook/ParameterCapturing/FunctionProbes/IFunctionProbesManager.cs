// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
{
    internal interface IFunctionProbesManager : IDisposable
    {
        public Task StartCapturingAsync(IList<MethodInfo> methods, IFunctionProbes probes, CancellationToken token);

        public Task StopCapturingAsync(CancellationToken token);

        public event EventHandler<InstrumentedMethod> OnProbeFault;
    }
}
