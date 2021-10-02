﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsReleaseTool.Impl;

namespace DiagnosticsReleaseTool.CommandLine
{
    class DiagnosticsReleaseCommandLine
    {
        static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder()
                .AddCommand(PrepareRelease())
                .CancelOnProcessTermination()
                .UseDefaults()
                .Build();

            return await parser.InvokeAsync(args);
        }

        public static Command PrepareRelease() =>
            new Command(
                name: "prepare-release",
                description: "Given a darc drop, generates validated manifests and layouts to initiate a tool release.")
            {
                CommandHandler.Create<Config, bool, CancellationToken>(DiagnosticsReleaseRunner.PrepareRelease),
                // Inputs
                InputDropPathOption(), ToolManifestPathOption(), ReleaseNameOption(),
                // Toggles
                ToolManifestVerificationOption(), DiagnosticLoggingOption(),
                // Outputs
                StagingPathOption(), AzureStorageAccountNameOption(), AzureStorageAccountKeyOption(), AzureStorageContainerNameOption(), AzureStorageSasExpirationOption()
            };

        private static Option<bool> DiagnosticLoggingOption() =>
            new Option<bool>(
                aliases: new[] { "-v", "--verbose" },
                description: "Enables diagnostic logging",
                getDefaultValue: () => false);

        private static Option ToolManifestPathOption() =>
            new Option<FileInfo>(
                aliases: new[] { "--tool-manifest", "-t" },
                description: "Full path to the manifest of tools and packages to publish.")
            {
                IsRequired = true
            }.ExistingOnly();

        private static Option<string> ReleaseNameOption() =>
            new Option<string>(
                aliases: new[] { "-r", "--release-name" },
                description: "Name of this release.")
            {
                IsRequired = true,
            };

        private static Option<bool> ToolManifestVerificationOption() =>
            new Option<bool>(
                alias: "--verify-tool-manifest",
                description: "Verifies that the assets being published match the manifest",
                getDefaultValue: () => true);

        private static Option<DirectoryInfo> InputDropPathOption() => 
            new Option<DirectoryInfo>(
                aliases: new[] { "-i", "--input-drop-path" },
                description: "Path to drop generated by `darc gather-drop`")
            {
                IsRequired = true
            }.ExistingOnly();

        private static Option StagingPathOption() =>
            new Option<DirectoryInfo>(
                aliases: new[] { "--staging-directory", "-s" },
                description: "Full path to the staging path.",
                getDefaultValue: () => new DirectoryInfo(
                    Path.Join(Path.GetTempPath(), Path.GetRandomFileName())))
            .LegalFilePathsOnly();

        private static Option<string> AzureStorageAccountNameOption() =>
            new Option<string>(
                aliases: new[] { "-n", "--account-name" },
                description: "Storage account name, must be in public azure cloud.")
            {
                IsRequired = true, 
            };

        private static Option<string> AzureStorageAccountKeyOption() =>
            new Option<string>(
                aliases: new[] { "-k", "--account-key" },
                description: "Storage account key, in base 64 format.")
            {
                IsRequired = true,
            };

        private static Option<string> AzureStorageContainerNameOption() =>
            new Option<string>(
                aliases: new[] { "-c", "--container-name" },
                description: "Storage account container name where the files will be uploaded.",
                getDefaultValue: () => "dotnet-monitor");

        private static Option<int> AzureStorageSasExpirationOption() =>
            new Option<int>(
                aliases: new[] { "--sas-valid-days" },
                description: "Number of days to allow access to the blobs via the provided SAS URIs.",
                getDefaultValue: () => 14); // default to just over 2 weeks
    }
}
