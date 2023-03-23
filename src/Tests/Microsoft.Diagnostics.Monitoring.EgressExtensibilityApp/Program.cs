// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Configuration;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
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

            Command egressCmd = new Command("Egress");

            egressCmd.Action = new CliActionWithExitCode(Egress);

            rootCommand.Add(egressCmd);

            return rootCommand.Invoke(args);
        }

        private static int Egress(InvocationContext context)
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

        // Due to https://github.com/dotnet/command-line-api/pull/2095, returning an exit code is "pay-for-play". A custom CliAction
        // must be implemented in order to actually return the exit code.
        private sealed class CliActionWithExitCode : CliAction
        {
            private Func<InvocationContext, int> _action;

            public CliActionWithExitCode(Func<InvocationContext, int> action)
            {
                ArgumentNullException.ThrowIfNull(action);

                _action = action;
            }

            public override int Invoke(InvocationContext context)
            {
                return _action(context);
            }

            public override Task<int> InvokeAsync(InvocationContext context, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
