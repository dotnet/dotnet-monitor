// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ProgramExtension : IExtension, IEgressExtension
    {
        private readonly string _path;
        private readonly ILogger<ProgramExtension> _logger;

        public ProgramExtension(string path, ILogger<ProgramExtension> logger)
        {
            _path = path;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => Path.GetDirectoryName(_path);

        /// <inheritdoc/>
        public async Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Func<Stream, CancellationToken, Task> getStreamAction, CancellationToken token)
        {
            // This _should_ only be used in this method, it can get moved to a constants class if that changes
            const string CommandArgProviderName = "--Provider-Name";
            // This is really weird, yes, but this is one of 2 overloads for [Stream].WriteAsync(...) that supports a CancellationToken, so we use a ReadOnlyMemory<char> instead of a string.
            ReadOnlyMemory<char> NewLine = new ReadOnlyMemory<char>("\r\n".ToCharArray());

            /* [TODOs]
             * 1. [Done] Add a new service to dynamically find these extension(s)
             * 2. [Done] Remove all raw logging statements from this method and refactor into LoggingExtensions
             * 3. Stream StdOut and StdErr async in the process so their streams don't need to end before we can return
             * 4. [Done] Refactor WaitForExit to do an async wait
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
            pStart.ArgumentList.Add(ExtensionTypes.Egress);
            pStart.ArgumentList.Add(CommandArgProviderName);
            pStart.ArgumentList.Add(configPayload.ProviderName);

            Process p = new Process()
            {
                StartInfo = pStart,
            };

            using OutputParser<EgressArtifactResult> parser = new(p, _logger);

            _logger.ExtensionStarting(_path, pStart.Arguments);
            p.Start();
            await JsonSerializer.SerializeAsync<ExtensionEgressPayload>(p.StandardInput.BaseStream, configPayload, options: null, token);
            await p.StandardInput.WriteAsync(NewLine, token);
            await p.StandardInput.BaseStream.FlushAsync(token);
            _logger.ExtensionConfigured(_path, p.Id);

            await getStreamAction(p.StandardInput.BaseStream, token);
            await p.StandardInput.BaseStream.FlushAsync(token);
            p.StandardInput.Close();
            _logger.ExtensionEgressPayloadCompleted(p.Id);

            await p.WaitForExitAsync(token);
            _logger.ExtensionExited(p.Id, p.ExitCode);

            return null;
        }
    }
}
