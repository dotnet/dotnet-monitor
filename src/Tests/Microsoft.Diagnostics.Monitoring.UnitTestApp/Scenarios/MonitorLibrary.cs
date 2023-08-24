// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class MonitorLibrary
    {
        [DllImport(ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        public static extern int TestHook([MarshalAs(UnmanagedType.FunctionPtr)] Action callback);

        public static void InitializeResolver()
        {
            NativeLibrary.SetDllImportResolver(typeof(MonitorLibrary).Assembly, ResolveDllImport);
        }

        public static IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            //DllImport for Windows automatically loads in-memory modules (such as the profiler). This is not the case for Linux/MacOS.
            //If we fail resolving the DllImport, we have to load the profiler ourselves.

            string profilerName = ProfilerHelper.GetPath(RuntimeInformation.ProcessArchitecture);
            if (NativeLibrary.TryLoad(profilerName, out IntPtr handle))
            {
                return handle;
            }

            return IntPtr.Zero;
        }
    }
}
