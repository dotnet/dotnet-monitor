// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.StartupHook;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class MonitorLibrary
    {
        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        public static extern int TestHook([MarshalAs(UnmanagedType.FunctionPtr)] Action callback);

        public static void InitializeResolver()
        {
            ProfilerResolver.InitializeResolver(typeof(MonitorLibrary));
        }
    }
}
