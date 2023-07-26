// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public partial class TestDotNetHost
    {
        private static Lazy<bool> s_HasHostFullPath =
            new(() => File.Exists(GetHostFullPath()));

        // The version is in the Major.Minor.Patch-label format; remove the label
        // and only parse the Major.Minor.Patch part.
        private static Lazy<Version> s_runtimeVersionLazy =
            new(() => Version.Parse(CurrentNetCoreVersionString.Split("-")[0]));

        public static Version RuntimeVersion =>
            s_runtimeVersionLazy.Value;

        public static string ExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        public static string ExeNameWithoutExtension => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            Path.GetFileNameWithoutExtension(ExeName) :
            ExeName;

        public static bool HasHostFullPath => s_HasHostFullPath.Value;

        public static string GetPath(Architecture? arch = null)
        {
            string hostPath = GetHostFullPath(arch);

            if (File.Exists(hostPath))
            {
                return hostPath;
            }

            // If the current repo enlistment has only ever been built and tested with Visual Studio,
            // the repo's private copy of dotnet will have never been setup.
            //
            // In this scenario fall back to the system's copy.
            // Limit this fallback behavior to only happen when running under Visual Studio.
            // (i.e. when on Windows and a well-defined VS-specific environment variable set)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSAPPIDDIR")))
            {
                Console.WriteLine($"'{hostPath}' does not exist, falling back to the system's version.");
                return ExeName;
            }

            throw new FileNotFoundException(null, hostPath);
        }

        public static string GetHostFullPath(Architecture? arch = null)
        {
            // e.g. <TEST_DOTNET_ROOT>/dotnet
            string dotnetDirPath = Environment.GetEnvironmentVariable("TEST_DOTNET_ROOT");

            if (string.IsNullOrEmpty(dotnetDirPath))
            {
                // e.g. <repoPath>/.dotnet
                dotnetDirPath = Path.Combine("..", "..", "..", "..", "..", ".dotnet");

                if (arch.HasValue && arch.Value != RuntimeInformation.OSArchitecture)
                {
                    // e.g. Append "\x86" to the path
                    dotnetDirPath = Path.Combine(dotnetDirPath, arch.Value.ToString("G").ToLowerInvariant());
                }
            }

            return Path.GetFullPath(Path.Combine(dotnetDirPath, ExeName));
        }

        public static TargetFrameworkMoniker BuiltTargetFrameworkMoniker
        {
            get
            {
                // Update with specific TFM when building this assembly for a new target framework.
#if NETCOREAPP3_1
                return TargetFrameworkMoniker.NetCoreApp31;
#elif NET5_0
                return TargetFrameworkMoniker.Net50;
#elif NET6_0
                return TargetFrameworkMoniker.Net60;
#elif NET7_0
                return TargetFrameworkMoniker.Net70;
#elif NET8_0
                return TargetFrameworkMoniker.Net80;
#endif
            }
        }
    }
}
