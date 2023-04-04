// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    internal sealed class EgressHelper
    {
        private static Stream StdInStream;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();

        internal static Command CreateEgressCommand<TOptions>(EgressProvider<TOptions> provider, Action<ExtensionEgressPayload, TOptions, ILogger> configureOptions = null) where TOptions : class, new()
        {
            Command egressCmd = new Command("Egress", "The class of extension being invoked; Egress is for egressing an artifact.");

            egressCmd.SetAction((result, token) => Egress(provider, token, configureOptions));

            return egressCmd;
        }

        private static async Task<int> Egress<TOptions>(EgressProvider<TOptions> provider, CancellationToken token, Action<ExtensionEgressPayload, TOptions, ILogger> configureOptions = null) where TOptions : class, new()
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);

                using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole().SetMinimumLevel(configPayload.LogLevel);
                });
                ILogger logger = loggerFactory.CreateLogger<EgressHelper>();

                TOptions options = BuildOptions(configPayload, logger, configureOptions);

                var context = new ValidationContext(options);

                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(options, context, results, true))
                {
                    if (results.Count > 0)
                    {
                        throw new EgressException(results.First().ErrorMessage);
                    }
                }

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(logger,
                    options,
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

        private static TOptions BuildOptions<TOptions>(ExtensionEgressPayload configPayload, ILogger logger, Action<ExtensionEgressPayload, TOptions, ILogger> configureOptions = null) where TOptions : new()
        {
            TOptions options = GetOptions<TOptions>(configPayload);

            configureOptions?.Invoke(configPayload, options, logger);

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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel LogLevel { get; set; }
    }
}
