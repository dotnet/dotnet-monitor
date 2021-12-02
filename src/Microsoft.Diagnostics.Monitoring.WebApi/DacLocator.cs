// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private sealed class Runtime
        {
            public string Name { get; set; }

            public Version Version { get; set; }

            public string Path { get; set; }

            public string GetRuntimeDirectory() => System.IO.Path.Combine(Path, Version.ToString());
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

            var process = new Process() { StartInfo = processStartInfo };
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => source.TrySetResult(process.ExitCode);
            using IDisposable registration = token.Register(() => source.TrySetCanceled(token));

            process.Start();
            await source.Task;

            var runtimes = new List<Runtime>();
            string line = null;
            while ( (line = process.StandardOutput.ReadLine()) != null)
            {
                runtimes.Add(ParseRuntime(line));
            }

            //TODO This is good enough for scenarios where we know we can actually get this payload, such as an image that contains the app and
            //dotnet-monitor, but won't work very well when there are multiple runtimes installed.
            Runtime matchingRuntime = runtimes.Where(r => r.Name == "Microsoft.NETCore.App").OrderByDescending(r => r.Version).FirstOrDefault();
            if (matchingRuntime != null)
            {
                return matchingRuntime.GetRuntimeDirectory();
            }
            return null;
        }

        private static Runtime ParseRuntime(string line)
        {
            //Microsoft.NETCore.App 6.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]

            const char Space = ' ';
            int fxVersionIndex = line.IndexOf(Space);
            int endFxVersionIndex = line.IndexOf(Space, fxVersionIndex + 1);

            if (fxVersionIndex == -1 || endFxVersionIndex == -1)
            {
                throw new FormatException();
            }

            string name = line.Substring(0, fxVersionIndex);
            int pathStart = endFxVersionIndex + 2;

            string version = line.Substring(fxVersionIndex + 1, endFxVersionIndex - fxVersionIndex - 1);
            string path = line.Substring(pathStart, line.Length - pathStart - 1);

            return new Runtime { Name = name, Version = new Version(version), Path = path };
        }
    }
}
