// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal sealed class DefaultDotnetToolsFileSystem :
        IDotnetToolsFileSystem
    {
        private const string DotnetFolderName = ".dotnet";
        private const string ToolsFolderName = "tools";

        // Location where extensions are stored by default.
        // Windows: "%USERPROFILE%\.dotnet\Tools"
        // Other: "%XDG_CONFIG_HOME%/.dotnet/tools" OR "%HOME%/.dotnet/tools" -> THIS HAS NOT BEEN TESTED YET ON LINUX
        private static readonly string DotnetToolsExtensionDirectoryPath =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DotnetFolderName, ToolsFolderName) :
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DotnetFolderName, ToolsFolderName);

        public string Path { get => DotnetToolsExtensionDirectoryPath; set => throw new NotImplementedException(); }
    }
}
