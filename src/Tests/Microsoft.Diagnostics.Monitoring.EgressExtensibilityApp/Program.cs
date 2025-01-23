// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Extensions.Configuration;
using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal sealed class Program
    {
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            Command executeCommand = new Command("Execute", "Execute is for egressing an artifact.");
            executeCommand.SetAction((result, token) => Egress(token));

            Command egressCommand = new Command("Egress", "The class of extension being invoked.")
            {
                executeCommand
            };

            rootCommand.Add(egressCommand);

            return rootCommand.Parse(args).Invoke();
        }

        private static async Task<int> Egress(CancellationToken token)
        {
            EgressArtifactResult result = new();
            try
            {
                ExtensionEgressPayload configPayload = await EgressHelper.GetPayload(token);
                TestEgressProviderOptions options = BuildOptions(configPayload);

                Assert.Single(options.Metadata.Keys);
                Assert.Equal(options.Metadata[EgressExtensibilityTestsConstants.Key], EgressExtensibilityTestsConstants.Value);

                if (options.ShouldSucceed)
                {
                    result.Succeeded = true;
                    result.ArtifactPath = EgressExtensibilityTestsConstants.SampleArtifactPath;
                }
                else
                {
                    result.Succeeded = false;
                    result.FailureMessage = EgressExtensibilityTestsConstants.SampleFailureMessage;
                }
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            string jsonBlob = JsonSerializer.Serialize(result);
            Console.WriteLine(jsonBlob);

            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static TestEgressProviderOptions BuildOptions(ExtensionEgressPayload configPayload)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            var configurationRoot = builder.AddInMemoryCollection(configPayload.Configuration).Build();

            TestEgressProviderOptions options = new();

            configurationRoot.Bind(options);

            return options;
        }
    }
}
