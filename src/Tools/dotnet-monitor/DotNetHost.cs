// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static partial class DotNetHost
    {
        private const string DotNetName = "dotnet";
        public const string ExecutableRootName = DotNetName;
        public const string InstallationDirectoryName = DotNetName;

        private static readonly IDotNetHostHelper Helper =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            new WindowsDotNetHostHelper() :
            new UnixDotNetHostHelper();

        private static readonly Lazy<string> PathLazy =
            new Lazy<string>(GetPath, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string ExecutablePath => PathLazy.Value;

        private static string GetPath()
        {
            // If current executable is already dotnet, return its path
            string? executablePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(executablePath) &&
                executablePath.EndsWith(Helper.ExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                return executablePath;
            }

            // Host locating algorithm based on how apphost would look up the dotnet root:
            // https://github.com/dotnet/runtime/blob/728fd85bc7ad04f5a0ea2ad0d4d8afe371ff9b64/src/native/corehost/fxr_resolver.cpp#L55

            // Get dotnet root from environment variable
            // TODO: check architecture specific environment variables (e.g. *_X86, *_X64, *_ARM64)
            string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (string.IsNullOrEmpty(dotnetRoot) &&
                !Helper.TryGetSelfRegisteredDirectory(out dotnetRoot) &&
                !Helper.TryGetDefaultInstallationDirectory(out dotnetRoot))
            {
                throw new DirectoryNotFoundException();
            }

            executablePath = System.IO.Path.Combine(dotnetRoot, Helper.ExecutableName);
            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException(null, executablePath);
            }
            return executablePath;
        }

        private interface IDotNetHostHelper
        {
            bool TryGetSelfRegisteredDirectory([NotNullWhen(true)] out string? dotnetRoot);

            bool TryGetDefaultInstallationDirectory([NotNullWhen(true)] out string? dotnetRoot);

            string ExecutableName { get; }
        }
    }
}
