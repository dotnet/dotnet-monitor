// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal static class MonitorExecutionContextTracker
    {
        private sealed class MonitorScopeTracker : IDisposable
        {
            private long _disposedState;

            public MonitorScopeTracker()
            {
                MarkExecutionContext(isMonitor: true);
            }

            public void Dispose()
            {
                if (!DisposableHelper.CanDispose(ref _disposedState))
                    return;

                MarkExecutionContext(isMonitor: false);
            }
        }

        private static readonly AsyncLocal<uint> _isMonitorTask = new();

        public static bool IsInMonitorContext()
        {
            return _isMonitorTask.Value != 0;
        }

        public static IDisposable MonitorScope()
        {
            return new MonitorScopeTracker();
        }

        private static void MarkExecutionContext(bool isMonitor)
        {
            if (isMonitor)
            {
                _isMonitorTask.Value++;
            }
            else
            {
                if (_isMonitorTask.Value == 0)
                {
                    Debug.Fail("Invalid ref count, would underflow");
                    return;
                }
                _isMonitorTask.Value--;
            }
        }
    }
}
