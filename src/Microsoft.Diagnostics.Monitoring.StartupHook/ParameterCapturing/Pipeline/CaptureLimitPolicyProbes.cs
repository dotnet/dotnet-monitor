// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Pipeline
{
    internal sealed class CaptureLimitPolicyProbes : IFunctionProbes
    {
        private readonly IFunctionProbes _probes;
        private readonly int _captureLimit;
        private readonly TaskCompletionSource _stopRequest;

        private int _captureCount;
        private bool _stopped;

        public CaptureLimitPolicyProbes(IFunctionProbes probes, int captureLimit, TaskCompletionSource stopRequest)
        {
            _probes = probes;
            _captureLimit = captureLimit;
            _stopRequest = stopRequest;
        }

        public void CacheMethods(IList<MethodInfo> methods) => _probes.CacheMethods(methods);

        public bool EnterProbe(ulong uniquifier, object[] args)
        {
            // In addition to the stop request, use a flag to more quickly react to the limit being reached,
            // limiting the amount of extra data being captured which is important in the case of hot paths.
            if (_stopped)
            {
                return false;
            }

            bool didCapture = _probes.EnterProbe(uniquifier, args);
            if (didCapture && Interlocked.Increment(ref _captureCount) == _captureLimit)
            {
                _stopped = true;
                _ = _stopRequest.TrySetResult();
            }

            return didCapture;
        }
    }
}
