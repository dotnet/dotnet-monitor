// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    public static class FunctionProbesStub
    {
        private delegate void EnterProbeDelegate(ulong uniquifier, object[] args);
        private static readonly EnterProbeDelegate s_fixedEnterProbeDelegate = EnterProbeStub;

        [ThreadStatic]
        private static int s_inProbeCount;

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
                PauseProbingForCurrentThread();
                _ = probes.EnterProbe(uniquifier, args);
            }
            finally
            {
                ResumeProbingForCurrentThread();
            }
        }

        public static bool IsProbingEnabled() => s_inProbeCount == 0;

        public static void PauseProbingForCurrentThread()
        {
            s_inProbeCount++;
        }

        public static void ResumeProbingForCurrentThread()
        {
            if (s_inProbeCount == 0)
            {
                throw new InvalidOperationException($"{nameof(ResumeProbingForCurrentThread)} was called more times than {nameof(PauseProbingForCurrentThread)}!");
            }
            s_inProbeCount--;
        }
    }
}
