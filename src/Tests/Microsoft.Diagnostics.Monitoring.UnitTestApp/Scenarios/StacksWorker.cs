// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal sealed class StacksWorker : IDisposable
    {
        private EventWaitHandle _eventWaitHandle = new ManualResetEvent(false);

        public sealed class StacksWorkerNested<T>
        {
            private WaitHandle _handle;

            public void DoWork<U>(U test, WaitHandle handle)
            {
                _handle = handle;
                MonitorLibrary.TestHook(Callback);
            }

            public void Callback()
            {
                HiddenFrameTestMethods.EntryPoint(() =>
                {
                    using EventSource eventSource = new EventSource("StackScenario");
                    using EventCounter eventCounter = new EventCounter("Ready", eventSource);
                    eventCounter.WriteMetric(1.0);
                    _handle.WaitOne();
                });
            }
        }

        public void Work()
        {
            StacksWorkerNested<int> nested = new StacksWorkerNested<int>();

            nested.DoWork<long>(5, _eventWaitHandle);
        }

        public void Signal()
        {
            _eventWaitHandle.Set();
        }

        public void Dispose()
        {
            _eventWaitHandle.Dispose();

        }
    }
}
