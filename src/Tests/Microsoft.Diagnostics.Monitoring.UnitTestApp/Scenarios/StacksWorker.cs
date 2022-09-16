﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal class StacksWorker : IDisposable
    {
        private EventWaitHandle _eventWaitHandle = new ManualResetEvent(false);

        public class StacksWorkerNested<T>
        {
            private WaitHandle _handle;

            public void DoWork<U>(U test, WaitHandle handle)
            {
                _handle = handle;
                StacksScenario.TestHook(Callback);
            }

            public void Callback()
            {
                _handle.WaitOne();
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
