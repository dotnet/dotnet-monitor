// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.Pipeline
{
    internal sealed class TestFunctionProbes : IFunctionProbes
    {
        private readonly Func<ulong, object[], bool>? _onEnterProbe;
        private readonly Action<IList<MethodInfo>>? _onCacheMethods;

        public TestFunctionProbes(Func<ulong, object[], bool>? onEnterProbe = null, Action<IList<MethodInfo>>? onCacheMethods = null)
        {
            _onEnterProbe = onEnterProbe;
            _onCacheMethods = onCacheMethods;
        }


        public void CacheMethods(IList<MethodInfo> methods)
        {
            _onCacheMethods?.Invoke(methods);
        }

        public bool EnterProbe(ulong uniquifier, object[] args)
        {
            return _onEnterProbe != null
                ? _onEnterProbe(uniquifier, args)
                : true;
        }
    }
}
