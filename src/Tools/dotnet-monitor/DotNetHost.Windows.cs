// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    partial class DotNetHost
    {
        private sealed class WindowsDotNetHostHelper : IDotNetHostHelper
        {
            public string ExecutableName => "dotnet.exe";

            public bool TryGetDefaultInstallationDirectory(out string dotnetRoot)
            {
                // CONSIDER: Account for emulation (emulated path would be under ".\x64\")
                dotnetRoot = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
                return true;
            }

            public bool TryGetSelfRegisteredDirectory(out string dotnetRoot)
            {
                // TODO: Lookup dotnet installation from registry
                dotnetRoot = null;
                return false;
            }
        }
    }
}
