// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    partial class DotNetHost
    {
        private sealed class WindowsDotNetHostHelper : IDotNetHostHelper
        {
            public string ExecutableName => FormattableString.Invariant($"{ExecutableRootName}.exe");

            public bool TryGetDefaultInstallationDirectory([NotNullWhen(true)] out string? dotnetRoot)
            {
                // CONSIDER: Account for emulation (emulated path would be under ".\x64\")
                dotnetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), InstallationDirectoryName);
                return true;
            }

            public bool TryGetSelfRegisteredDirectory([NotNullWhen(true)] out string? dotnetRoot)
            {
                // CONSIDER: Lookup dotnet installation from registry
                dotnetRoot = null;
                return false;
            }
        }
    }
}
