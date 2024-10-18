// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class HiddenFrameTestMethods
    {
        // We keep the entry and exit points visible so they act as sentinel frames
        // when checking output that excludes hidden frames.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EntryPoint(Action work)
        {
            PartiallyVisibleClass partiallyVisibleClass = new();
            partiallyVisibleClass.DoWorkFromVisibleDerivedClass(work);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ExitPoint(Action work)
        {
            work();
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DoWorkFromHiddenMethod(Action work)
        {
            ExitPoint(work);
        }

        [StackTraceHidden]
        public abstract class BaseHiddenClass
        {
#pragma warning disable CA1822 // Mark members as static
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void DoWorkFromHiddenBaseClass(Action work)
#pragma warning restore CA1822 // Mark members as static
            {
                DoWorkFromHiddenMethod(work);
            }
        }

        public class PartiallyVisibleClass : BaseHiddenClass
        {
            // StackTraceHidden attributes are not inherited
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void DoWorkFromVisibleDerivedClass(Action work)
            {
                DoWorkFromHiddenBaseClass(work);
            }
        }
    }
}
