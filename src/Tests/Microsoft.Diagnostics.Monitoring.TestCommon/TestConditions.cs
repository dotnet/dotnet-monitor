// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class TestConditions
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsNotAlpine => !DistroInformation.IsAlpineLinux;

        public static bool IsNotWindows => !IsWindows;

        public static bool IsNotArm64 => RuntimeInformation.OSArchitecture != Architecture.Arm64;
    }
}
