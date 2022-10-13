// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal class Program
    {
        private static Stream StdInStream = null;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();
        static async Task<int> Main(string[] args)
        {
            // Expected command line format is: AzureBlobStorage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to Azure Storage.");

            Command egressCmd = new Command("Egress", "The class of extension being invoked; Egress is for egressing an artifact.")
            { };

            egressCmd.SetHandler(Program.Egress);

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

                ILoggerFactory loggerFactory = new LoggerFactory();
                ILogger<AzureBlobEgressProvider> myLogger = loggerFactory.CreateLogger<AzureBlobEgressProvider>();

                AzureBlobEgressProvider provider = new AzureBlobEgressProvider();

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
<<<<<<< HEAD
=======

        private static Dictionary<string, string> GetDictionaryConfig(Dictionary<string, object> configDict, string propKey)
        {
            if (configDict.ContainsKey(propKey))
            {
                configDict[propKey].GetType();

                Console.WriteLine("Type: " + configDict[propKey].GetType());
                Console.WriteLine("Value: " + configDict[propKey]);

                // THIS IS WORKING -> NORMAL METADATA ISN'T SHOWING UP THOUGH
                if (configDict[propKey] is JsonElement element)
                {
                    var dict =  JsonSerializer.Deserialize<Dictionary<string, string>>(element);

                    var dictTrimmed = (from kv in dict
                                       where kv.Value != null
                                           select kv).ToDictionary(kv => kv.Key, kv => kv.Value); // Doesn't have keys that correspond to null

                    var dictToReturn = new Dictionary<string, string>();

                    foreach (string key in dictTrimmed.Keys)
                    {
                        string updatedKey = key.Split(":").Last();
                        dictToReturn.Add(updatedKey, dictTrimmed[key]);
                    }

                    return dictToReturn;

                }
            }
            return null;
        }
>>>>>>> 1e71968a (Removing some unused files; still works)
    }

    internal class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public string ProfileName { get; set; }
    }
}
