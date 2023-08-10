// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline
{
    public sealed class ScopeGuard : IDisposable
    {
        private long _disposedState;
        private readonly Action _uninitialize;

        public ScopeGuard(Action initialize, Action uninitialize)
        {
            initialize();
            _uninitialize = uninitialize;
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _uninitialize();
        }
    }
}
