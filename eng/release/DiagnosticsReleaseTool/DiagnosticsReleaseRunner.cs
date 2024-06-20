// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsReleaseTool.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReleaseTool.Core;

namespace DiagnosticsReleaseTool.Impl
{
#pragma warning disable CA1052 // We use this type for logging.
    internal sealed class DiagnosticsReleaseRunner
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        internal const string ManifestName = "publishManifest.json";

        internal static async Task<int> PrepareRelease(Config releaseConfig, bool verbose, bool dryRun, CancellationToken ct)
        {
            // TODO: This will throw if invalid drop path is given.
            DarcHelpers darcLayoutHelper = new(releaseConfig.DropPath);

            ILogger logger = GetDiagLogger(verbose);

            List<ILayoutWorker> layoutWorkerList = new()
            {
                // TODO: We may want to inject a logger.
                new NugetLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName, DarcHelpers.IsNuGetPackage),
                new SymbolPackageLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName),
                new ChecksumLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName),
                new SkipLayoutWorker(shouldHandleFileFunc: DiagnosticsRepoHelpers.IsDockerUtilityFile),
                // This should always be last since it will accept any file
                new BlobLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName)
            };

            List<IReleaseVerifier> verifierList = new() { };

            if (releaseConfig.ShouldVerifyManifest)
            {
                // TODO: add verifier.
                // verifierList.Add();
            }

            // TODO: Probably should use BAR ID instead as an identifier for the metadata to gather.
            ReleaseMetadata releaseMetadata = darcLayoutHelper.GetDropMetadataForSingleRepoVariants(DiagnosticsRepoHelpers.RepositoryUrls);
            DirectoryInfo basePublishDirectory = darcLayoutHelper.GetShippingDirectoryForSingleProjectVariants(DiagnosticsRepoHelpers.ProductNames);
            string publishManifestPath = Path.Combine(releaseConfig.StagingDirectory.FullName, ManifestName);

            IPublisher releasePublisher = dryRun ?
                new SkipPublisher() :
                new AzureBlobBublisher(releaseConfig.AccountName, releaseConfig.ClientId, releaseConfig.ContainerName, releaseConfig.ReleaseName, logger);

            IManifestGenerator manifestGenerator = new DiagnosticsManifestGenerator(releaseMetadata, releaseConfig.ToolManifest, logger);

            using Release diagnosticsRelease = new(
                productBuildPath: basePublishDirectory,
                layoutWorkers: layoutWorkerList,
                verifiers: verifierList,
                publisher: releasePublisher,
                manifestGenerator: manifestGenerator,
                manifestSavePath: publishManifestPath
            );

            diagnosticsRelease.UseLogger(logger);

            return await diagnosticsRelease.RunAsync(ct);
        }

        private static ILogger GetDiagLogger(bool verbose)
        {
            IConfigurationRoot loggingConfiguration = new ConfigurationBuilder()
                .AddJsonFile("logging.json", optional: false, reloadOnChange: false)
                .Build();

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => {
                builder.AddConfiguration(loggingConfiguration.GetSection("Logging"))
                    .AddConsole();

                if (verbose)
                {
                    builder.AddFilter(
                        "DiagnosticsReleaseTool.Impl.DiagnosticsReleaseRunner",
                        LogLevel.Trace);
                }
            });

            return loggerFactory.CreateLogger<DiagnosticsReleaseRunner>();
        }
    }
}
