// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    partial class DotNetHost
    {
        private sealed class UnixDotNetHostHelper : IDotNetHostHelper
        {
            private const string DefaultPathLinux = $"/usr/share/{InstallationDirectoryName}";
            private const string DefaultPathOSX = $"/usr/local/share/{InstallationDirectoryName}";
            private const string InstallationFilePath = $"/etc/{InstallationDirectoryName}/install_location";

            private static readonly string CurrentArchInstallationFilePath =
                FormattableString.Invariant($"{InstallationFilePath}_{RuntimeInformation.ProcessArchitecture.ToString("G").ToLowerInvariant()}");

            public string ExecutableName => ExecutableRootName;

            public bool TryGetDefaultInstallationDirectory([NotNullWhen(true)] out string? dotnetRoot)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // CONSIDER: Account for emulation (emulated path would be under "./x64/")
                    dotnetRoot = DefaultPathOSX;
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dotnetRoot = DefaultPathLinux;
                    return true;
                }

                dotnetRoot = null;
                return false;
            }

            public bool TryGetSelfRegisteredDirectory([NotNullWhen(true)] out string? dotnetRoot)
            {
                return TryReadFileFirstLine(CurrentArchInstallationFilePath, out dotnetRoot) ||
                    TryReadFileFirstLine(InstallationFilePath, out dotnetRoot);
            }

            private static bool TryReadFileFirstLine(string filePath, [NotNullWhen(true)] out string? content)
            {
                try
                {
                    using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using StreamReader reader = new StreamReader(stream);
                    content = reader.ReadLine();
                    return !string.IsNullOrEmpty(content);
                }
                catch
                {
                    content = null;
                    return false;
                }
            }
        }
    }
}
