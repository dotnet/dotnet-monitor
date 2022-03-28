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
        static async Task Main(string[] args)
        {
            string jsonConfig = Console.ReadLine();

            Console.WriteLine("ReadJson: " + jsonConfig);

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

            EgressArtifactSettings artifactSettings = new EgressArtifactSettings()
            {
                ContentType = ContentTypes.ApplicationOctetStream,
                Name = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}",                
            };

            AzureBlobEgressProvider provider = new AzureBlobEgressProvider();

            //Console.TreatControlCAsInput = true;
            Console.CancelKeyPress += Console_CancelKeyPress;

            string blobUri = await provider.EgressAsync(options, action, config.Settings, CancellationToken.None);
            Console.Write(blobUri);
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

}