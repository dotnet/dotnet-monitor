// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class DacLocator
    {
        private const string NetCoreApp = "Microsoft.NETCore.App";
        private const char VersionSeparator = '-';

        private sealed class Runtime
        {
            public string Name { get; set; }

            public Version Version { get; set; }

            public string VersionSuffix { get; set; }

            public string Path { get; set; }

            public string GetRuntimeDirectory()
            {
                string versionFolder = Version.ToString();
                if (!string.IsNullOrEmpty(VersionSuffix))
                {
                    versionFolder = string.Concat(versionFolder, VersionSeparator, VersionSuffix);
                }
                return System.IO.Path.Combine(Path, versionFolder);
            }
        }

        public static async Task<string> LocateRuntime(CancellationToken token)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                FileName = "dotnet",
                RedirectStandardOutput = true,
            };
            //CONSIDER We could get the location of the runtime dll for a specific process by collecting rundown instead.

            using var process = new Process() { StartInfo = processStartInfo };
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => source.TrySetResult(process.ExitCode);
            using IDisposable registration = token.Register(() => source.TrySetCanceled(token));

            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return null;
            }

            int exitCode = await source.Task;
            if (exitCode != 0)
            {
                return null;
            }

            var runtimes = new List<Runtime>();
            string line = null;
            while ( (line = process.StandardOutput.ReadLine()) != null)
            {
                if (TryParseRuntime(line, out Runtime runtime))
                {
                    runtimes.Add(runtime);
                }
            }

            //TODO This is good enough for scenarios where we know we can actually get this payload, such as an image that contains the app and
            //dotnet-monitor, but won't work very well when there are multiple runtimes installed.
            Runtime matchingRuntime = runtimes
                .Where(r => r.Name == NetCoreApp)
                .OrderByDescending(r => r.Version)
                .ThenByDescending(r => r.VersionSuffix, Comparer<string>.Create(CompareRuntimeSuffix))
                .FirstOrDefault();
            if (matchingRuntime != null)
            {
                return matchingRuntime.GetRuntimeDirectory();
            }
            return null;
        }

        private static int CompareRuntimeSuffix(string left, string right)
        {
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            {
                return 0;
            }

            //Left is greater than right. Empty Version suffixes are greater than preview builds.
            if (string.IsNullOrEmpty(left))
            {
                return 1;
            }
            if (string.IsNullOrEmpty(right))
            {
                return -1;
            }
            return Comparer<string>.Default.Compare(left, right);
        }

        private static bool TryParseRuntime(string line, out Runtime runtime)
        {
            //Microsoft.NETCore.App 6.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
            //Microsoft.NETCore.App 7.0.0-preview.7.2 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]

            runtime = null;
            const char Space = ' ';

            int fxVersionIndex = line.IndexOf(Space);
            int endFxVersionIndex = line.IndexOf(Space, fxVersionIndex + 1);

            if (fxVersionIndex == -1 || endFxVersionIndex == -1)
            {
                return false;
            }

            string name = line.Substring(0, fxVersionIndex);
            int pathStart = endFxVersionIndex + 2;

            string version = line.Substring(fxVersionIndex + 1, endFxVersionIndex - fxVersionIndex - 1);
            string path = line.Substring(pathStart, line.Length - pathStart - 1);
            string suffix = string.Empty;

            if (version.Contains(VersionSeparator))
            {
                string[] versionParts = version.Split(VersionSeparator);
                version = versionParts[0];
                suffix = versionParts[1];
            }

            runtime = new Runtime { Name = name, Version = new Version(version), Path = path, VersionSuffix = suffix };
            return true;
        }
    }
}
