// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class DistroInformation
    {
        private const string LinuxReleaseFilePath = "/etc/os-release";
        private const string AlpineLinuxIdLine = "ID=alpine";

        public static bool IsAlpineLinux
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return false;

                if (!File.Exists(LinuxReleaseFilePath))
                    return false;

                string releaseInfo = null;
                try
                {
                    releaseInfo = File.ReadAllText(LinuxReleaseFilePath);
                }
                catch
                {
                }

                return !string.IsNullOrWhiteSpace(releaseInfo) &&
                    releaseInfo.Contains(AlpineLinuxIdLine);
            }
        }
    }
}
