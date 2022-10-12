// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal class Program
    {
        private static Stream StdInStream = null;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();
        static async Task<int> Main(string[] args)
        {
            // Expected command line format is: AzureBlobStorage.exe Egress --Provider-Name MyProviderEndpointName
            RootCommand rootCommand = new RootCommand("Egresses an artifact to Azure Storage.");

            var providerNameOption = new Option<string>(
                name: "--Provider-Name",
                description: "The provider name given in the configuration to dotnet-monitor.");

            Command egressCmd = new Command("Egress", "The class of extension being invoked; Egress is for egressing an artifact.")
            { providerNameOption };

            egressCmd.SetHandler(async (providerNameOption) => { await Egress(providerNameOption); }, providerNameOption);

            rootCommand.Add(egressCmd);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Egress(string ProviderName)
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
                AzureBlobEgressProviderOptions options = BuildOptions(configPayload);

                AzureBlobEgressProvider provider = new AzureBlobEgressProvider(logger: null); // TODO: Replace logger with real instance of console logger (console events on standard out will get forwarded to dotnet-monitor and written to it's console).

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(EgressProviderTypes.AzureBlobStorage, configPayload.ProfileName, options, GetStream, configPayload.Settings, CancelSource.Token);
                result.Succeeded = true;
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            string jsonBlob = JsonSerializer.Serialize<EgressArtifactResult>(result);
            Console.Write(jsonBlob);

            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static AzureBlobEgressProviderOptions BuildOptions(ExtensionEgressPayload configPayload)
        {
            AzureBlobEgressProviderOptions options = new AzureBlobEgressProviderOptions()
            {
                AccountUri = GetUriConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.AccountUri)),
                AccountKey = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.AccountKey)),
                AccountKeyName = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.AccountKeyName)),
                SharedAccessSignature = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature)),
                SharedAccessSignatureName = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.SharedAccessSignatureName)),
                ContainerName = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.ContainerName)),
                BlobPrefix = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.BlobPrefix)),
                QueueName = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.QueueName)),
                QueueAccountUri = GetUriConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.QueueAccountUri)),
            };

            if (string.IsNullOrEmpty(options.AccountKey) && !string.IsNullOrEmpty(options.AccountKeyName) && configPayload.Properties.TryGetValue(options.AccountKeyName, out string accountKey))
            {
                options.AccountKey = accountKey;
            }

            if (string.IsNullOrEmpty(options.SharedAccessSignature) && !string.IsNullOrEmpty(options.SharedAccessSignatureName) && configPayload.Properties.TryGetValue(options.SharedAccessSignatureName, out string sasSig))
            {
                options.SharedAccessSignature = sasSig;
            }

            return options;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            CancelSource.Cancel();
            StdInStream.Close();
        }

        private static Task<Stream> GetStream(CancellationToken cancellationToken)
        {
            StdInStream = Console.OpenStandardInput();
            return Task.FromResult(StdInStream);
        }

        private static string GetConfig(Dictionary<string, string> configDict, string propKey)
        {
            if (configDict.ContainsKey(propKey))
            {
                return configDict[propKey];
            }
            return null;
        }
        private static Uri GetUriConfig(Dictionary<string, string> configDict, string propKey)
        {
            string uriStr = GetConfig(configDict, propKey);
            if (uriStr == null)
            {
                return null;
            }
            return new Uri(uriStr);
        }
    }

    internal class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public string ProfileName { get; set; }
    }

}
