// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    internal sealed class SharedEntrypoint<T> where T : class, IEgressProviderOptions, new()
    {
        private static Stream StdInStream;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();

        private static EgressProvider _provider;
        private static Action<ExtensionEgressPayload, T> _configureOptions;

        public static async Task<int> Entrypoint(string[] args, EgressProvider provider, Action<ExtensionEgressPayload, T> configureOptions)
        {
            _provider = provider;
            _configureOptions = configureOptions;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Expected command line format is: dotnet-monitor-egress-*extension_type*.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to cloud storage.");

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
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
                IEgressProviderOptions options = BuildOptions(configPayload);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await _provider.EgressAsync(options,
                    GetStream,
                    configPayload.Settings,
                    CancelSource.Token);

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

        private static T BuildOptions(ExtensionEgressPayload configPayload)
        {
            T options = GetOptions(configPayload);

            _configureOptions(configPayload, options);

            return options;
        }

        private static T GetOptions(ExtensionEgressPayload payload)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            var configurationRoot = builder.AddInMemoryCollection(payload.Configuration).Build();

            T options = new();

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
