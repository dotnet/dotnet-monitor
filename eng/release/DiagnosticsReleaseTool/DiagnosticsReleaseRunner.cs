// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DiagnosticsReleaseTool.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReleaseTool.Core;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsReleaseTool.Impl
{
    internal class DiagnosticsReleaseRunner
    {
        internal const string ManifestName = "publishManifest.json";

        internal async static Task<int> PrepareRelease(Config releaseConfig, bool verbose, bool dryRun, CancellationToken ct)
        {
            // TODO: This will throw if invalid drop path is given.
            var darcLayoutHelper = new DarcHelpers(releaseConfig.DropPath);

            ILogger logger = GetDiagLogger(verbose);

            var layoutWorkerList = new List<ILayoutWorker>
            {
                // TODO: We may want to inject a logger.
                new NugetLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName, DarcHelpers.IsNuGetPackage),
                new SymbolPackageLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName),
                new ChecksumLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName),
                new SkipLayoutWorker(shouldHandleFileFunc: DiagnosticsRepoHelpers.IsDockerUtilityFile),
                // This should always be last since it will accept any file
                new BlobLayoutWorker(stagingPath: releaseConfig.StagingDirectory.FullName)
            };

            var verifierList = new List<IReleaseVerifier> { };

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
                new AzureBlobBublisher(releaseConfig.AccountName, releaseConfig.ClientId, releaseConfig.ContainerName, releaseConfig.BuildVersion, logger);
            IManifestGenerator manifestGenerator = new DiagnosticsManifestGenerator(releaseMetadata, releaseConfig.ToolManifest, logger);

            using var diagnosticsRelease = new Release(
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
            var loggingConfiguration = new ConfigurationBuilder()
                .AddJsonFile("logging.json", optional: false, reloadOnChange: false)
                .Build();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(loggingConfiguration.GetSection("Logging"))
                    .AddConsole();

                if (verbose)
                {
                    builder.AddFilter("DiagnosticsReleaseTool.Impl.DiagnosticsReleaseRunner", LogLevel.Trace);
                }
            });

            return loggerFactory.CreateLogger<DiagnosticsReleaseRunner>();
        }
    }
}
