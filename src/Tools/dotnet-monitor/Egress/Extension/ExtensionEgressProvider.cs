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
            Dictionary<string, string> props = new Dictionary<string, string>();
            foreach (string key in _propertyProvider.GetKeys())
            {
                if (_propertyProvider.TryGetPropertyValue(key, out string val))
                {
                    props.Add(key, val);
                }
            }

            ExtensionEgressPayload payload = new ExtensionEgressPayload()
            {
                Settings = artifactSettings,
                Configuration = options,
                Properties = props,
                ProfileName = providerName,
            };

            // [TODO] add a new service to dynamically find these extensions
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

            await action(p.StandardInput.BaseStream, token);
            p.StandardInput.BaseStream.Flush();

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
