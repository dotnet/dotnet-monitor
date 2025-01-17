// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal static class MonitorExecutionContextTracker
    {
        private sealed class MonitorScopeTracker : IDisposable
        {
            private long _disposedState;
            private readonly bool _didMarkContext;

            public MonitorScopeTracker()
            {
                _didMarkContext = MarkExecutionContext(isMonitor: true);
            }

            public void Dispose()
            {
                if (!DisposableHelper.CanDispose(ref _disposedState) || !_didMarkContext)
                    return;

                _ = MarkExecutionContext(isMonitor: false);
            }
        }

        private static readonly AsyncLocal<uint> _monitorScopeCount = new();

        public static bool IsInMonitorContext()
        {
            return _monitorScopeCount.Value != 0;
        }

        public static IDisposable MonitorScope()
        {
            return new MonitorScopeTracker();
        }

        private static bool MarkExecutionContext(bool isMonitor)
        {
            if (isMonitor)
            {
                //
                // Overflows should never happen unless we have bugs around leaking MonitorScopeTracker or creating
                // an unreasonable number of them in a single execution context / its children.
                //
                // No-op to gracefully handle any such bugs.
                //
                if (_monitorScopeCount.Value == uint.MaxValue)
                {
                    Debug.Fail($"{nameof(_monitorScopeCount)} would overflow");
                    return false;
                }

                _monitorScopeCount.Value++;
            }
            else
            {
                //
                // Underflows should never happen since this method is private and only called to decrement
                // if there's a corresponding MonitorScopeTracker being disposed which would have incremented.
                // Gracefully handle regardless.
                //
                if (_monitorScopeCount.Value == uint.MinValue)
                {
                    Debug.Fail($"{nameof(_monitorScopeCount)} would underflow");
                    return false;
                }

                _monitorScopeCount.Value--;
            }

            return true;
        }
    }
}
