// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CommandLine;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal class Program
    {
        private static Stream StdInStream = null;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();

        protected static ILogger Logger { get; set; }

        static async Task<int> Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            Logger = loggerFactory.CreateLogger<Program>();

            // Expected command line format is: dotnet-monitor-egress-azureblobstorage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to Azure Storage.");

            Command egressCmd = new Command("Egress", "The class of extension being invoked; Egress is for egressing an artifact.");

            egressCmd.SetHandler(Egress);

            rootCommand.Add(egressCmd);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Egress()
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
                AzureBlobEgressProviderOptions options = BuildOptions(configPayload);

                AzureBlobEgressProvider provider = new AzureBlobEgressProvider(Logger);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(options, GetStream, configPayload.Settings, CancelSource.Token);
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
                QueueSharedAccessSignature = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.QueueSharedAccessSignature)),
                QueueSharedAccessSignatureName = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.QueueSharedAccessSignatureName)),
                ManagedIdentityClientId = GetConfig(configPayload.Configuration, nameof(AzureBlobEgressProviderOptions.ManagedIdentityClientId))
            };

            // If account key was not provided but the name was provided,
            // lookup the account key property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.AccountKey) && !string.IsNullOrEmpty(options.AccountKeyName))
            {
                if (configPayload.Properties.TryGetValue(options.AccountKeyName, out string accountKey))
                {
                    options.AccountKey = accountKey;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.AccountKeyName);
                }
            }

            // If shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.SharedAccessSignature) && !string.IsNullOrEmpty(options.SharedAccessSignatureName))
            {
                if (configPayload.Properties.TryGetValue(options.SharedAccessSignatureName, out string sasSig))
                {
                    options.SharedAccessSignature = sasSig;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.SharedAccessSignatureName);
                }
            }

            // If queue shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.QueueSharedAccessSignature) && !string.IsNullOrEmpty(options.QueueSharedAccessSignatureName))
            {
                if (configPayload.Properties.TryGetValue(options.QueueSharedAccessSignatureName, out string signature))
                {
                    options.QueueSharedAccessSignature = signature;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.QueueSharedAccessSignatureName);
                }
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
        public string ProviderName { get; set; }
    }
}
