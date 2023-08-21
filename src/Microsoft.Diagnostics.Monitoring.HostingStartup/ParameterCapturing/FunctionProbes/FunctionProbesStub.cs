// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    public static class FunctionProbesStub
    {
        private delegate void EnterProbeDelegate(ulong uniquifier, object[] args);
        private static readonly EnterProbeDelegate s_fixedEnterProbeDelegate = EnterProbeStub;

        [ThreadStatic]
        private static bool s_inProbe;

        internal static ReadOnlyDictionary<ulong, InstrumentedMethod>? InstrumentedMethodCache { get; set; }

        internal static IFunctionProbes? Instance { get; set; }

        internal static ulong GetProbeFunctionId()
        {
            return s_fixedEnterProbeDelegate.Method.GetFunctionId();
        }

        public static void EnterProbeStub(ulong uniquifier, object[] args)
        {
            IFunctionProbes? probes = Instance;
            if (probes == null || s_inProbe)
            {
                return;
            }

            try
            {
                s_inProbe = true;
                probes.EnterProbe(uniquifier, args);
            }
            finally
            {
                s_inProbe = false;
            }
        }
    }
}
