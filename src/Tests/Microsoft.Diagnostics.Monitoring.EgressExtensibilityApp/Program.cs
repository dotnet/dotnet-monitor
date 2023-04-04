// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Configuration;
using System;
using System.CommandLine;
using System.Text.Json;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal sealed class Program
    {
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            Command egressCmd = new Command("Egress");

            egressCmd.SetAction(Egress);

            rootCommand.Add(egressCmd);

            return rootCommand.Parse(args).Invoke();
        }

        private static int Egress(ParseResult parseResult)
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();

                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
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
