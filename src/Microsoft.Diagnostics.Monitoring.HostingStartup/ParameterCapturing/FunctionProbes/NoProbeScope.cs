// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    // Creates a scope in which parameter capturing is disabled.
    // It should never be used in async code!
    internal sealed class NoProbeScope : IDisposable
    {
#if DEBUG
        private readonly int scopeThreadId = Environment.CurrentManagedThreadId;
#endif
        public NoProbeScope()
        {
            FunctionProbesStub.PauseProbingForCurrentThread();
        }

        public void Dispose()
        {
#if DEBUG
            if (Environment.CurrentManagedThreadId != scopeThreadId)
            {
                throw new InvalidOperationException($"{nameof(NoProbeScope)} should be disposed on the same thread as where it was constructed.");
            }
#endif
            FunctionProbesStub.ResumeProbingForCurrentThread();
        }
    }
}
