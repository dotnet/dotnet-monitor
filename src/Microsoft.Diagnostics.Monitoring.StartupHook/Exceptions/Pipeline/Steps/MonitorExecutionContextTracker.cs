// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal static class MonitorExecutionContextTracker
    {
        private sealed class MonitorScopeTracker : IDisposable
        {
            private long _disposedState;

            public MonitorScopeTracker()
            {
                MarkMonitorThread(isMonitor: true);
            }

            public void Dispose()
            {
                if (!DisposableHelper.CanDispose(ref _disposedState))
                    return;

                MarkMonitorThread(isMonitor: false);
            }
        }

        private static readonly AsyncLocal<bool> _isMonitorTask = new();
        private static readonly ThreadLocal<uint> _isMonitorThread = new();

        public static bool IsInMonitorContext()
        {
            return (_isMonitorThread.IsValueCreated && _isMonitorThread.Value != 0) || _isMonitorTask.Value;
        }

        public static void MarkMonitorTask(bool isMonitor = true)
        {
            _isMonitorTask.Value = isMonitor;
        }

        public static void MarkMonitorThread(bool isMonitor = true)
        {
            if (isMonitor)
            {
                _isMonitorThread.Value++;

            }
            else
            {
                _isMonitorThread.Value--;
            }
        }

        public static IDisposable MonitorScope()
        {
            return new MonitorScopeTracker();
        }
    }
}
