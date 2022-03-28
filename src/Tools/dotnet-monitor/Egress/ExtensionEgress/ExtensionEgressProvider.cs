// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
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
            string providerCategory,
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

            string startingPayload = JsonSerializer.Serialize<ExtensionEgressPayload>(payload);

            // [TODO] add a new service to dynamically find these extensions
            ProcessStartInfo pStart = new ProcessStartInfo()
            {
                FileName = "D:\\repos\\github\\dotnet-monitor-kelltrick\\artifacts\\bin\\Microsoft.Diagnostics.Monitoring.AzureStorage\\Debug\\net6.0\\azure-storage.exe",
                RedirectStandardInput = true,
            };
            Process p = new Process()
            {
                StartInfo = pStart,
            };

            Logger?.LogInformation("Starting process...");
            p.Start();
            Logger?.LogInformation("starting stream...");

            p.StandardInput.WriteLine(startingPayload);

            await action(p.StandardInput.BaseStream, token);
            p.StandardInput.BaseStream.Flush();

            Logger?.LogInformation("Stream done, sending close...");

            p.StandardInput.Close();

            string procContent = await p.StandardOutput.ReadToEndAsync();

            Logger?.LogInformation("Waiting for exit...");
            p.WaitForExit();
            Logger?.LogInformation($"Exited with code: {p.ExitCode}");

            return procContent;
        }
    }
}
