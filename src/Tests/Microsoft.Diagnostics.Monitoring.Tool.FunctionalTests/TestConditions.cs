// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class TestConditions
    {
        public static bool IsDumpSupported
        {
            get
            {
                // MacOS supported dumps starting in .NET 5
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major < 5)
                    return false;

                // MacOS dumps inconsistently segfault the runtime on .NET 5: https://github.com/dotnet/dotnet-monitor/issues/174
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major == 5)
                    return false;

                return true;
            }
        }

        public static bool IsNetCore31 => DotNetHost.BuiltTargetFrameworkMoniker == TargetFrameworkMoniker.NetCoreApp31;

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsNotWindows => !IsWindows;
    }
}
