// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class NativeLibraryHelper
    {
        private const string ConfigurationName =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        private const string OSReleasePath = "/etc/os-release";

        public static string GetSharedLibraryPath(Architecture architecture, string rootName)
        {
            string artifactsBinPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", ".."));
            return Path.Combine(artifactsBinPath, GetNativeBinDirectoryName(architecture), GetSharedLibraryName(rootName));
        }

        public static string GetTargetRuntimeIdentifier(Architecture? architecture)
        {
            string architectureString = GetArchitectureFolderName(architecture ?? RuntimeInformation.OSArchitecture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FormattableString.Invariant($"win-{architectureString}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return FormattableString.Invariant($"osx-{architectureString}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (File.Exists(OSReleasePath) && File.ReadAllText(OSReleasePath).Contains("Alpine", StringComparison.OrdinalIgnoreCase))
                {
                    return FormattableString.Invariant($"linux-musl-{architectureString}");
                }
                else
                {
                    return FormattableString.Invariant($"linux-{architectureString}");
                }
            }

            throw new PlatformNotSupportedException("Unable to determine OS platform.");
        }

        private static string GetNativeBinDirectoryName(Architecture architecture)
        {
            return $"{GetTargetRuntimeIdentifier(architecture)}.{ConfigurationName}";
        }

        private static string GetSharedLibraryName(string rootName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{rootName}.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"lib{rootName}.so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"lib{rootName}.dylib";
            }
            throw new PlatformNotSupportedException();
        }

        private static string GetArchitectureFolderName(Architecture architecture)
        {
            return architecture.ToString("G").ToLowerInvariant();
        }
    }
}
