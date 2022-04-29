// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ProgramExtension : IExtension, IEgressExtension
    {
        private readonly string _path;

        public ProgramExtension(string path)
        {
            _path = path;
        }

        public string Name => Path.GetDirectoryName(_path);

        public async Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Stream payload)
        {
            /* [TODOs]
             * 1. Add a new service to dynamically find these extension(s)
             * 2. Remove all raw logging statements from this method and refactor into LoggingExtensions
             * 3. Stream StdOut and StdErr async in the process so their streams don't need to end before we can return
             * 4. Refactor WaitForExit to do an async wait
             * 5. Add well-factored protocol for returning information from an extension
             */
            ProcessStartInfo pStart = new ProcessStartInfo()
            {
                FileName = _path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process p = new Process()
            {
                StartInfo = pStart,
            };

            Logger?.LogInformation("Starting process...");
            p.Start();
            Logger?.LogInformation("starting stream...");

            await JsonSerializer.SerializeAsync<ExtensionEgressPayload>(p.StandardInput.BaseStream, payload, options: null, token);
            await p.StandardInput.WriteLineAsync();
            await p.StandardInput.BaseStream.FlushAsync(token);

            await action(p.StandardInput.BaseStream, token);
            await p.StandardInput.BaseStream.FlushAsync(token);

            Logger?.LogInformation("Stream done, sending close...");

            p.StandardInput.Close();

            Logger?.LogInformation("Waiting for exit...");

            string procStdOutContent = await p.StandardOutput.ReadToEndAsync();
            string procErrorContent = await p.StandardError.ReadToEndAsync();

            await p.WaitForExitAsync(token);
            Logger?.LogInformation($"Exited with code: {p.ExitCode}");

            if (!string.IsNullOrWhiteSpace(procErrorContent))
            {
                Logger?.LogWarning($"Process Error output: {procErrorContent}");
            }

            return procStdOutContent;
        }
    }
}
