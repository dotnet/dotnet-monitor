// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class PerFunctionProbeWrapper
    {
        Action<object[]> _probe;
        private int _invokeCount;

        public PerFunctionProbeWrapper(Action<object[]> probe)
        {
            _probe = probe;
        }

        public int GetInvokeCount()
        {
            return _invokeCount;
        }

        public void Invoke(object[] args)
        {
            Interlocked.Increment(ref _invokeCount);
            _probe(args);
        }
    }
    internal sealed class PerFunctionProbeProxy : IFunctionProbes
    {
        private ConcurrentDictionary<ulong, PerFunctionProbeWrapper> _perFunctionProbes = new();

        public void RegisterPerFunctionProbe(MethodInfo method, Action<object[]> probe)
        {
            _perFunctionProbes[method.GetFunctionId()] = new PerFunctionProbeWrapper(probe);
        }

        public void ClearPerFunctionProbe(MethodInfo method)
        {
            _perFunctionProbes[method.GetFunctionId()] = null;
        }

        public void ClearAllProbes()
        {
            _perFunctionProbes.Clear();
        }

        public int GetProbeInvokeCount(MethodInfo method)
        {
            if (!_perFunctionProbes.TryGetValue(method.GetFunctionId(), out PerFunctionProbeWrapper probe))
            {
                return 0;
            }

            return probe.GetInvokeCount();
        }

        public void EnterProbe(ulong uniquifier, object[] args)
        {
            if (!_perFunctionProbes.TryGetValue(uniquifier, out PerFunctionProbeWrapper probe))
            {
                return;
            }

            probe.Invoke(args);
        }
    }
}
