// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static partial class DotNetHost
    {
        private static readonly string ExecutableName =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            "dotnet.exe" :
            "dotnet";

        private static readonly Lazy<string> PathLazy =
            new Lazy<string>(GetPath, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string Path => PathLazy.Value;

        private static string GetPath()
        {
            // If current executable is already dotnet, return its path
            string executablePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(executablePath) && executablePath.EndsWith(ExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                return executablePath;
            }

            // Get dotnet root from environment variable
            // TODO: check architecture specific environment variables (e.g. *_X86, *_X64, *_ARM64)
            string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot))
            {
                executablePath = System.IO.Path.Combine(dotnetRoot, ExecutableName);
                if (!File.Exists(executablePath))
                {
                    throw new FileNotFoundException(null, executablePath);
                }
                return executablePath;
            }

            // Rely on PATH lookup
            return ExecutableName;
        }
    }
}
