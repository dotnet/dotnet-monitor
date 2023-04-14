// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
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

        internal static CliCommand CreateEgressCommand<TProvider, TOptions>(Action<IServiceCollection> configureServices = null)
            where TProvider : EgressProvider<TOptions>
            where TOptions : class, new()
        {
            CliCommand egressCmd = new CliCommand("Egress", "The class of extension being invoked; Egress is for egressing an artifact.");

            egressCmd.SetAction((result, token) => Egress<TProvider, TOptions>(configureServices, token));

            return egressCmd;
        }

        private static async Task<int> Egress<TProvider, TOptions>(Action<IServiceCollection> configureServices, CancellationToken token)
            where TProvider : EgressProvider<TOptions>
            where TOptions : class, new()
        {
            // Design Points:
            // - The serialization model is separate from the parts that interact directly with the egress implementation.
            //   For example, the egress implementations are not able to access the ExtensionEgressPayload instance. This
            //   allows changing the serialization format and structure without affecting the egress implementations.
            // - The use of dependency injection allows the caller to contribute their custom services that are unknown
            //   to the egress infrastructure; it further eliminates the need for this method to understand how to validate
            //   egress options (barring the generic data annotation validator), how to further configure egress options
            //   (if anything additional is necessary), creating and passing optional services such as loggers, etc.
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();
                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);

                ServiceCollection services = CreateServices<TOptions>(configPayload, configureServices);

                // Attempt to register the egress provider if not already registered; this allows the service configuration
                // callback to register the egress provider if it has additional requirements that cannot be fulfilled by
                // dependency injection.
                services.TryAddSingleton<EgressProvider<TOptions>, TProvider>();

                services.MakeReadOnly();

                await using ServiceProvider serviceProvider = services.BuildServiceProvider();

                EgressProvider<TOptions> provider = serviceProvider.GetRequiredService<EgressProvider<TOptions>>();
                TOptions options = serviceProvider.GetRequiredService<IOptionsSnapshot<TOptions>>().Get(configPayload.ProviderName);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(
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

            string jsonBlob = JsonSerializer.Serialize(result);
            Console.Write(jsonBlob);

            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static ServiceCollection CreateServices<TOptions>(ExtensionEgressPayload payload, Action<IServiceCollection> configureServices)
            where TOptions : class, new()
        {
            ServiceCollection services = new();

            // Logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(payload.LogLevel);
                builder.AddConsole();
            });

            // Options configuration, validation, etc
            services.AddOptions<TOptions>(payload.ProviderName)
                .Configure(options =>
                {
                    IConfigurationBuilder builder = new ConfigurationBuilder();
                    builder
                        .AddInMemoryCollection(payload.Configuration)
                        .Build()
                        .Bind(options);
                });
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();

            services.AddSingleton(new EgressProperties(payload.Properties));

            // Optionally allow additional services; this allows the caller to participate in dependency injection
            // and fulfillment of services that the common egress infrastructure has no knowledge about.
            configureServices?.Invoke(services);

            return services;
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
