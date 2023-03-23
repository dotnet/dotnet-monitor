// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    internal sealed class EgressHelper
    {
        private static Stream StdInStream;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();

        internal static Command CreateEgressCommand<TOptions>(EgressProvider<TOptions> provider, Action<ExtensionEgressPayload, TOptions> configureOptions = null) where TOptions : class, new()
        {
            Command egressCmd = new Command("Egress", "The class of extension being invoked; Egress is for egressing an artifact.");

            egressCmd.SetActionWithExitCode((context, token) => Egress(provider, token, configureOptions));

            return egressCmd;
        }

        private static async Task<int> Egress<TOptions>(EgressProvider<TOptions> provider, CancellationToken token, Action<ExtensionEgressPayload, TOptions> configureOptions = null) where TOptions : class, new()
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
                TOptions options = BuildOptions(configPayload, configureOptions);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(options,
                    GetStream,
                    configPayload.Settings,
                    token);

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

        private static TOptions BuildOptions<TOptions>(ExtensionEgressPayload configPayload, Action<ExtensionEgressPayload, TOptions> configureOptions = null) where TOptions : new()
        {
            TOptions options = GetOptions<TOptions>(configPayload);

            configureOptions?.Invoke(configPayload, options);

            return options;
        }

        private static TOptions GetOptions<TOptions>(ExtensionEgressPayload payload) where TOptions : new()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            var configurationRoot = builder.AddInMemoryCollection(payload.Configuration).Build();

            TOptions options = new();

            configurationRoot.Bind(options);

            return options;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            CancelSource.Cancel();
            StdInStream.Close();
        }

        private static async Task GetStream(Stream outputStream, CancellationToken cancellationToken)
        {
            const int DefaultBufferSize = 0x10000;

            StdInStream = Console.OpenStandardInput();
            await StdInStream.CopyToAsync(outputStream, DefaultBufferSize, cancellationToken);
        }
    }

    internal sealed class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public string ProviderName { get; set; }
    }
}
