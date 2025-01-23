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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    public sealed class EgressHelper
    {
        private static Stream StdInStream;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();
        private const int ExpectedPayloadProtocolVersion = 1;

        public static Command CreateEgressCommand<TProvider, TOptions>(Action<IServiceCollection> configureServices = null)
            where TProvider : EgressProvider<TOptions>
            where TOptions : class, new()
        {
            Command executeCommand = new Command("Execute", "Execute is for egressing an artifact.");
            executeCommand.SetAction((result, token) => Egress<TProvider, TOptions>(configureServices, token));

            Command validateCommand = new Command("Validate", "Validate is for validating an extension's options on configuration change.");
            validateCommand.SetAction((result, token) => Validate<TProvider, TOptions>(configureServices, token));

            Command egressCommand = new Command("Egress", "The class of extension being invoked.")
            {
                executeCommand,
                validateCommand
            };

            return egressCommand;
        }


        private static async Task<int> Egress<TProvider, TOptions>(Action<IServiceCollection> configureServices, CancellationToken token)
            where TProvider : EgressProvider<TOptions>
            where TOptions : class, new()
        {
            EgressArtifactResult result = new();
            try
            {
                ExtensionEgressPayload configPayload = await GetPayload(token);
                await using var serviceProvider = BuildServiceProvider<TProvider, TOptions>(configureServices, configPayload);

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

            return ProcessEgressResult(result);
        }

        private static async Task<int> Validate<TProvider, TOptions>(Action<IServiceCollection> configureServices, CancellationToken token)
            where TProvider : EgressProvider<TOptions>
            where TOptions : class, new()
        {
            EgressArtifactResult result = new();
            try
            {
                ExtensionEgressPayload configPayload = await GetPayload(token);
                await using var serviceProvider = BuildServiceProvider<TProvider, TOptions>(configureServices, configPayload);

                TOptions options = serviceProvider.GetRequiredService<IOptionsSnapshot<TOptions>>().Get(configPayload.ProviderName);

                result.ArtifactPath = string.Empty;
                result.Succeeded = true;
            }
            catch (OptionsValidationException ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            return ProcessEgressResult(result);
        }

        private static int ProcessEgressResult(EgressArtifactResult result)
        {
            string jsonBlob = JsonSerializer.Serialize(result);
            Console.Write(jsonBlob);
            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static ServiceProvider BuildServiceProvider<TProvider, TOptions>(Action<IServiceCollection> configureServices, ExtensionEgressPayload configPayload)
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

            ServiceCollection services = CreateServices<TOptions>(configPayload, configureServices);

            // Attempt to register the egress provider if not already registered; this allows the service configuration
            // callback to register the egress provider if it has additional requirements that cannot be fulfilled by
            // dependency injection.
            services.TryAddSingleton<EgressProvider<TOptions>, TProvider>();

#if NET7_0_OR_GREATER
            services.MakeReadOnly();
#endif

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        internal static async Task<ExtensionEgressPayload> GetPayload(CancellationToken token)
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
                    throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_IncorrectPayloadVersion, dotnetMonitorPayloadProtocolVersion, ExpectedPayloadProtocolVersion));
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

            return configPayload;
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

            await StdInStream.CopyToAsync(outputStream, DefaultBufferSize, cancellationToken);
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
