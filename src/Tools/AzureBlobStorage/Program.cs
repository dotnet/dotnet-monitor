// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal class Program
    {
        private static Stream StdInStream = null;
        static async Task<int> Main(string[] args)
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();

                ExtensionEgressPayload config = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);

                AzureBlobEgressProviderOptions options = new AzureBlobEgressProviderOptions()
                {
                    AccountUri = GetUriConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.AccountUri)),
                    AccountKey = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.AccountKey)),
                    AccountKeyName = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.AccountKeyName)),
                    SharedAccessSignature = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.SharedAccessSignature)),
                    SharedAccessSignatureName = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.SharedAccessSignatureName)),
                    ContainerName = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.ContainerName)),
                    BlobPrefix = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.BlobPrefix)),
                    QueueName = GetConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.QueueName)),
                    QueueAccountUri = GetUriConfig(config.Configuration, nameof(AzureBlobEgressProviderOptions.QueueAccountUri)),
                };

                if (options.AccountKey == null && options.AccountKeyName != null && config.Properties.ContainsKey(options.AccountKeyName))
                {
                    options.AccountKey = config.Properties[options.AccountKeyName];
                }

                if (options.SharedAccessSignature == null && options.SharedAccessSignatureName != null && config.Properties.ContainsKey(options.SharedAccessSignatureName))
                {
                    options.SharedAccessSignature = config.Properties[options.SharedAccessSignatureName];
                }

                Func<CancellationToken, Task<Stream>> action = new Func<CancellationToken, Task<Stream>>(
                    (CancellationToken token) =>
                    {
                        StdInStream = Console.OpenStandardInput();
                        return Task.FromResult(StdInStream);
                    });
                
                AzureBlobEgressProvider provider = new AzureBlobEgressProvider(null);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(EgressProviderTypes.AzureBlobStorage, config.ProfileName, options, action, config.Settings, CancellationToken.None);
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

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            StdInStream.Close();
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