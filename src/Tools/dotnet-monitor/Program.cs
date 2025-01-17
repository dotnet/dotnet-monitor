// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Commands;
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class Program
    {
        private static CliCommand GenerateApiKeyCommand()
        {
            CliCommand command = new CliCommand(
                name: "generatekey",
                description: Strings.HelpDescription_CommandGenerateKey)
            {
                OutputOption,
                ExpirationOption
            };

            command.SetAction((result) =>
            {
                GenerateApiKeyCommandHandler.Invoke(
                    result.GetValue(OutputOption),
                    result.GetValue(ExpirationOption),
                    result.Configuration.Output);
            });

            return command;
        }

        private static CliCommand CollectCommand()
        {
            CliCommand command = new CliCommand(
                name: "collect",
                description: Strings.HelpDescription_CommandCollect)
            {
                UrlsOption,
                MetricUrlsOption,
                ProvideMetricsOption,
                DiagnosticPortOption,
                NoAuthOption,
                TempApiKeyOption,
                NoHttpEgressOption,
                ConfigurationFilePathOption,
                ExitOnStdinDisconnect
            };

            command.SetAction((result, token) =>
            {
                return CollectCommandHandler.Invoke(
                    token,
                    result.GetValue(UrlsOption),
                    result.GetValue(MetricUrlsOption),
                    result.GetValue(ProvideMetricsOption),
                    result.GetValue(DiagnosticPortOption),
                    result.GetValue(NoAuthOption),
                    result.GetValue(TempApiKeyOption),
                    result.GetValue(NoHttpEgressOption),
                    result.GetValue(ConfigurationFilePathOption),
                    result.GetValue(ExitOnStdinDisconnect));
            });

            return command;
        }

        private static CliCommand ConfigCommand()
        {
            CliCommand showCommand = new CliCommand(
                name: "show",
                description: Strings.HelpDescription_CommandShow)
            {
                UrlsOption,
                MetricUrlsOption,
                ProvideMetricsOption,
                DiagnosticPortOption,
                NoAuthOption,
                TempApiKeyOption,
                NoHttpEgressOption,
                ConfigurationFilePathOption,
                ConfigLevelOption,
                ShowSourcesOption
            };

            showCommand.SetAction(result =>
            {
                ConfigShowCommandHandler.Invoke(
                    result.GetValue(UrlsOption),
                    result.GetValue(MetricUrlsOption),
                    result.GetValue(ProvideMetricsOption),
                    result.GetValue(DiagnosticPortOption),
                    result.GetValue(NoAuthOption),
                    result.GetValue(TempApiKeyOption),
                    result.GetValue(NoHttpEgressOption),
                    result.GetValue(ConfigurationFilePathOption),
                    result.GetValue(ConfigLevelOption),
                    result.GetValue(ShowSourcesOption));
            });

            CliCommand configCommand = new CliCommand(
                name: "config",
                description: Strings.HelpDescription_CommandConfig)
            {
                showCommand
            };

            return configCommand;
        }

        private static CliOption<string[]> UrlsOption =
            new CliOption<string[]>("--urls", "-u")
            {
                DefaultValueFactory = (_) => new[] { "https://localhost:52323" },
                Description = Strings.HelpDescription_OptionUrls,
                HelpName = "urls"
            };

        private static CliOption<string[]> MetricUrlsOption =
            new CliOption<string[]>("--metricUrls")
            {
                DefaultValueFactory = (_) => new[] { "http://localhost:52325" },
                Description = Strings.HelpDescription_OptionMetricsUrls,
                HelpName = "metricUrls"
            };

        private static CliOption<bool> ProvideMetricsOption =
            new CliOption<bool>("--metrics", "-m")
            {
                DefaultValueFactory = (_) => true,
                Description = Strings.HelpDescription_OptionMetrics,
                HelpName = "metrics"
            };

        private static CliOption<string> DiagnosticPortOption =
            new CliOption<string>("--diagnostic-port")
            {
                Description = Strings.HelpDescription_OptionDiagnosticPort,
                HelpName = "diagnosticPort"
            };

        private static CliOption<FileInfo> ConfigurationFilePathOption =
            new CliOption<FileInfo>("--configuration-file-path")
            {
                Description = Strings.HelpDescription_OptionConfigurationFilePath,
                HelpName = "configurationFilePath"
            };

        private static CliOption<bool> NoAuthOption =
            new CliOption<bool>("--no-auth")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionNoAuth,
                HelpName = "noAuth"
            };

        private static CliOption<bool> NoHttpEgressOption =
            new CliOption<bool>("--no-http-egress")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionNoHttpEgress,
                HelpName = "noHttpEgress"
            };

        private static CliOption<bool> TempApiKeyOption =
            new CliOption<bool>("--temp-apikey")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionTempApiKey,
                HelpName = "tempApiKey"
            };

        private static CliOption<OutputFormat> OutputOption =
            new CliOption<OutputFormat>("--output", "-o")
            {
                DefaultValueFactory = (_) => OutputFormat.Json,
                Description = Strings.HelpDescription_OutputFormat,
                HelpName = "output"
            };

        private static CliOption<TimeSpan> ExpirationOption =
            new CliOption<TimeSpan>("--expiration", "-e")
            {
                DefaultValueFactory = (_) => AuthConstants.ApiKeyJwtDefaultExpiration,
                Description = Strings.HelpDescription_Expiration,
                HelpName = "expiration"
            };

        private static CliOption<ConfigDisplayLevel> ConfigLevelOption =
            new CliOption<ConfigDisplayLevel>("--level")
            {
                DefaultValueFactory = (_) => ConfigDisplayLevel.Redacted,
                Description = Strings.HelpDescription_OptionLevel,
                HelpName = "level"
            };

        private static CliOption<bool> ShowSourcesOption =
            new CliOption<bool>("--show-sources")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionShowSources,
                HelpName = "showSources"
            };

        private static CliOption<bool> ExitOnStdinDisconnect =
            new CliOption<bool>("--exit-on-stdin-disconnect")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionExitOnStdinDisconnect
            };

        public static Task<int> Main(string[] args)
        {
            // Prevent child processes from inheriting startup hooks
            Environment.SetEnvironmentVariable(ToolIdentifiers.EnvironmentVariables.StartupHooks, null);

            TestAssemblies.SimulateStartupHook();

            CliRootCommand root = new()
            {
                CollectCommand(),
                ConfigCommand(),
                GenerateApiKeyCommand()
            };

            return root.Parse(args).InvokeAsync();
        }
    }

    internal enum ConfigDisplayLevel
    {
        Redacted,
        Full,
    }
}
