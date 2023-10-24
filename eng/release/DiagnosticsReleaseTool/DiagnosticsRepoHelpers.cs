// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace DiagnosticsReleaseTool.Util
{
    public static class DiagnosticsRepoHelpers
    {
        public static readonly string[] ProductNames = new[] { "dotnet-monitor", "dotnet-dotnet-monitor" };
        public static readonly string[] RepositoryUrls = new[] { "https://github.com/dotnet/dotnet-monitor", "https://dev.azure.com/dnceng/internal/_git/dotnet-dotnet-monitor" };
        internal static bool IsDockerUtilityFile(FileInfo arg) =>
            arg.FullName.EndsWith(".nupkg.version")
            || arg.FullName.EndsWith(".nupkg.buildversion");
    }
}
