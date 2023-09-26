// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    internal static class BuildOutput
    {
        public const string ConfigurationName =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        // This is the binary output directory when built from the dotnet-monitor repo: <repoRoot>/artifacts/bin
        public static readonly string RootPath =
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", ".."));
    }
}
