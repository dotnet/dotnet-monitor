// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    public static class FunctionProbesStub
    {
        private delegate void EnterProbeDelegate(ulong uniquifier, object[] args);
        private static readonly EnterProbeDelegate s_fixedEnterProbeDelegate = EnterProbeStub;

        private static readonly AsyncLocal<int> s_inProbeCount = new();

        internal static FunctionProbesState? State { get; set; }

        internal static ulong GetProbeFunctionId()
        {
            return s_fixedEnterProbeDelegate.Method.GetFunctionId();
        }

        public static void EnterProbeStub(ulong uniquifier, object[] args)
        {
            IFunctionProbes? probes = State?.Probes;
            if (probes == null || !IsProbingEnabled())
            {
                return;
            }

            try
            {
                PauseProbing();
                _ = probes.EnterProbe(uniquifier, args);
            }
            finally
            {
                ResumeProbing();
            }
        }

        public static bool IsProbingEnabled() => s_inProbeCount.Value == 0;

        public static void PauseProbing()
        {
            s_inProbeCount.Value++;
        }

        public static void ResumeProbing()
        {
            if (s_inProbeCount.Value == 0)
            {
                throw new InvalidOperationException($"{nameof(ResumeProbing)} was called more times than {nameof(PauseProbing)}!");
            }
            s_inProbeCount.Value--;
        }
    }
}
