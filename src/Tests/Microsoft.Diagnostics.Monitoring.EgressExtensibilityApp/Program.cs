// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal sealed class Program
    {
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            Command egressCmd = new Command("Egress");

            egressCmd.SetHandler(() => Egress());

            rootCommand.Add(egressCmd);

            return rootCommand.Invoke(args);
        }

        private static int Egress()
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();

                Console.WriteLine("Initial");

                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);

                Console.WriteLine("Config Payload: " + configPayload.Configuration.ToString());

                TestEgressProviderOptions options = BuildOptions(configPayload);

                Console.WriteLine("Options: " + options.ShouldSucceed);

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
            builder.SetBasePath(Directory.GetCurrentDirectory());

            Dictionary<string, string> configAsDict =
                JsonSerializer.Deserialize<Dictionary<string, string>>(configPayload.Configuration);

            var config = builder.AddInMemoryCollection(configAsDict).Build();

            IConfigurationSection section = config.GetSection("root");

            TestEgressProviderOptions options = new();

            section.Bind(options);

            return options;
        }
    }
}
