// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    internal static class TestConditions
    {
        public static bool IsDumpSupported
        {
            get
            {
                // Linux dumps currently broken by https://github.com/dotnet/diagnostics/issues/2098
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return false;

                // MacOS supported dumps starting in .NET 5
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major < 5)
                    return false;

                return true;
            }
        }

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
