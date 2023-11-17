// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal static class InProcFeatureExecutionContextTracker
    {
        private sealed class InProcFeatureScopeTracker : IDisposable
        {
            private long _disposedState;

            public InProcFeatureScopeTracker()
            {
                MarkInProcFeatureThread(inProcFeature: true);
            }

            public void Dispose()
            {
                if (!DisposableHelper.CanDispose(ref _disposedState))
                    return;

                MarkInProcFeatureThread(inProcFeature: false);
            }
        }

        private static readonly AsyncLocal<bool> _inProcFeature = new();
        private static readonly ThreadLocal<uint> _inProcFeatureThread = new();

        public static bool IsInProcFeatureContext()
        {
            return (_inProcFeatureThread.IsValueCreated && _inProcFeatureThread.Value != 0) || _inProcFeature.Value;
        }

        public static void MarkInProcFeatureTask(bool inProcFeature = true)
        {
            _inProcFeature.Value = inProcFeature;
        }

        public static void MarkInProcFeatureThread(bool inProcFeature = true)
        {
            if (inProcFeature)
            {
                _inProcFeatureThread.Value++;

            }
            else
            {
                _inProcFeatureThread.Value--;
            }
        }

        public static IDisposable InProcFeatureScope()
        {
            return new InProcFeatureScopeTracker();
        }
    }
}
