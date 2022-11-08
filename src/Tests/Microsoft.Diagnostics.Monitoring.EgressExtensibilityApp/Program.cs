// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal class Program
    {
        const int DefaultBufferSize = 4000;

        static async Task<int> Main(string[] args)
        {
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
                Console.WriteLine("JSONCONFIG: " + jsonConfig);
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);

                Stream outputStream = new MemoryStream(); // might not be a good stream type

                TimeSpan timeout = new TimeSpan(0, 0, 30); // Should do something real here
                CancellationTokenSource timeoutSource = new(timeout);

                outputStream = GetStream();

                Assert.Equal(outputStream.Length, DefaultBufferSize);

                TestEgressProviderOptions options = BuildOptions(configPayload);

                if (options.ShouldSucceed)
                {
                    result.Succeeded = true;
                    result.ArtifactPath = "/test/artifactPath"; // do something real here
                }
                else
                {
                    result.Succeeded = false;
                    result.FailureMessage = "The egress operation failed."; // do something real here
                }
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            string jsonBlob = JsonSerializer.Serialize<EgressArtifactResult>(result);
            Console.Write(jsonBlob);

            await Task.Delay(1); // TEMPORARY

            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static TestEgressProviderOptions BuildOptions(ExtensionEgressPayload configPayload)
        {
            TestEgressProviderOptions options = new TestEgressProviderOptions()
            {
                ShouldSucceed = GetConfig(configPayload.Configuration, nameof(TestEgressProviderOptions.ShouldSucceed)),
            };

            return options;
        }

        private static Stream GetStream()
        {
            byte[] buffer = new byte[DefaultBufferSize];

            Console.OpenStandardInput().Read(buffer, 0, DefaultBufferSize);

            return new MemoryStream(buffer);
        }

        private static bool GetConfig(IDictionary<string, string> configDict, string propKey)
        {
            if (configDict.ContainsKey(propKey))
            {
                return bool.Parse(configDict[propKey]);
            }

            // throw?

            return false;
        }
    }

}
