// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class ExtensionEgressProvider :
        EgressProvider<ExtensionEgressProviderOptions>
    {
        private readonly IEgressPropertiesProvider _propertyProvider;

        public ExtensionEgressProvider(IEgressPropertiesProvider propertyProvider, ILogger<ExtensionEgressProvider> logger)
            : base(logger)
        {
            _propertyProvider = propertyProvider;
        }

        public override async Task<string> EgressAsync(
            string providerType,
            string providerName,
            ExtensionEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            ExtensionEgressPayload payload = new ExtensionEgressPayload()
            {
                Settings = artifactSettings,
                Configuration = options,
                Properties = _propertyProvider.GetAllProperties(),
                ProviderName = providerName,
            };

            /* [TODOs]
             * 1. Add a new service to dynamically find these extension(s)
             * 2. Remove all raw logging statements from this method and refactor into LoggingExtensions
             * 3. Stream StdOut and StdErr async in the process so their streams don't need to end before we can return
             * 4. Refactor WaitForExit to do an async wait
             */
            const string extensionProcessPath = "MyPathTo\\Extension.exe";
            ProcessStartInfo pStart = new ProcessStartInfo()
            {
                FileName = extensionProcessPath,
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
            await p.StandardInput.BaseStream.FlushAsync();

            await action(p.StandardInput.BaseStream, token);
            await p.StandardInput.BaseStream.FlushAsync();

            Logger?.LogInformation("Stream done, sending close...");

            p.StandardInput.Close();

            Logger?.LogInformation("Waiting for exit...");

            string procStdOutContent = await p.StandardOutput.ReadToEndAsync();
            string procErrorContent = await p.StandardError.ReadToEndAsync();

            p.WaitForExit();
            Logger?.LogInformation($"Exited with code: {p.ExitCode}");

            if (!string.IsNullOrWhiteSpace(procErrorContent))
            {
                Logger?.LogWarning($"Process Error output: {procErrorContent}");
            }

            return procStdOutContent;
        }
    }
}
