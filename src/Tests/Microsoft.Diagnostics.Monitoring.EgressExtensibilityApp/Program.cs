// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Configuration;
using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal sealed class Program
    {
        private static Stream StdInStream;
        private const int ExpectedPayloadProtocolVersion = 1; // This should match what's in EgressExtension.cs

        static int Main(string[] args)
        {
            CliRootCommand rootCommand = new CliRootCommand();

            CliCommand egressCmd = new CliCommand("Egress");

            egressCmd.SetAction((result, token) => Egress(token));
            //egressCmd.SetAction(Egress);

            rootCommand.Add(egressCmd);

            return rootCommand.Parse(args).Invoke();
        }

        private static async Task<int> Egress(CancellationToken token)
        {
            EgressArtifactResult result = new();
            try
            {
                StdInStream = Console.OpenStandardInput();

                int dotnetMonitorPayloadProtocolVersion;
                long payloadLengthBuffer;
                byte[] payloadBuffer;

                using (BinaryReader reader = new BinaryReader(StdInStream, Encoding.UTF8, leaveOpen: true))
                {
                    dotnetMonitorPayloadProtocolVersion = reader.ReadInt32();
                    if (dotnetMonitorPayloadProtocolVersion != ExpectedPayloadProtocolVersion)
                    {
                        throw new ArgumentOutOfRangeException(nameof(dotnetMonitorPayloadProtocolVersion));
                    }

                    payloadLengthBuffer = reader.ReadInt64();

                    if (payloadLengthBuffer < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(payloadLengthBuffer));
                    }
                }

                payloadBuffer = new byte[payloadLengthBuffer];
                await ReadExactlyAsync(payloadBuffer, token);

                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(payloadBuffer);
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

        private static async Task ReadExactlyAsync(Memory<byte> buffer, CancellationToken token)
        {
#if NET7_0_OR_GREATER
            await StdInStream.ReadExactlyAsync(buffer, token);
#else
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await StdInStream.ReadAsync(buffer.Slice(totalRead), token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                totalRead += read;
            }
#endif
        }
    }
}
