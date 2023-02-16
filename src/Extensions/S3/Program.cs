// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.S3
{
    internal sealed class Program
    {
        private static Stream StdInStream;
        private static CancellationTokenSource CancelSource = new CancellationTokenSource();

        public static ILogger Logger { get; set; }

        static async Task<int> Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            Logger = loggerFactory.CreateLogger<Program>();

            // Expected command line format is: dotnet-monitor-egress-s3storage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to S3 storage.");

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
                S3StorageEgressProviderOptions options = BuildOptions(configPayload);

                S3StorageEgressProvider provider = new S3StorageEgressProvider(Logger);

                Console.CancelKeyPress += Console_CancelKeyPress;

                result.ArtifactPath = await provider.EgressAsync(options,
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

        private static S3StorageEgressProviderOptions BuildOptions(ExtensionEgressPayload configPayload)
        {
            S3StorageEgressProviderOptions options = GetOptions<S3StorageEgressProviderOptions>(configPayload);

            return options;
        }

        private static T GetOptions<T>(ExtensionEgressPayload payload) where T : class, new()
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
