// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class HiddenFrameTestMethods
    {
        /// <summary>
        /// Entry point to test frame visibility
        /// </summary>
        public class PartiallyVisibleClass : BaseHiddenClass
        {
            // StackTraceHidden attributes are not inherited
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void DoWorkEntryPoint(Action work)
            {
                DoWorkFromHiddenBaseClass(work);
            }
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DoWorkFromHiddenMethod(Action work)
        {
            work();
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
    }
}
