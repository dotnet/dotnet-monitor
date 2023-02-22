// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Commands;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class Program
    {
        private static Command GenerateApiKeyCommand()
        {
            Command command = new Command(
                name: "generatekey",
                description: Strings.HelpDescription_CommandGenerateKey)
            {
                OutputOption
            };

            command.SetHandler(async (context, token) =>
            {
                context.ExitCode = await GenerateApiKeyCommandHandler.Invoke(
                    token,
                    context.ParseResult.GetValue(OutputOption),
                    context.Console);
            });

            return command;
        }

        private static Command CollectCommand()
        {
            Command command = new Command(
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
                ConfigurationFilePathOption
            };

            command.SetHandler(async (context, token) =>
            {
                context.ExitCode = await CollectCommandHandler.Invoke(
                    token,
                    context.ParseResult.GetValue(UrlsOption),
                    context.ParseResult.GetValue(MetricUrlsOption),
                    context.ParseResult.GetValue(ProvideMetricsOption),
                    context.ParseResult.GetValue(DiagnosticPortOption),
                    context.ParseResult.GetValue(NoAuthOption),
                    context.ParseResult.GetValue(TempApiKeyOption),
                    context.ParseResult.GetValue(NoHttpEgressOption),
                    context.ParseResult.GetValue(ConfigurationFilePathOption));
            });

            return command;
        }

        private static Command ConfigCommand()
        {
            Command showCommand = new Command(
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

            showCommand.SetHandler(context =>
            {
                ConfigShowCommandHandler.Invoke(
                    context.ParseResult.GetValue(UrlsOption),
                    context.ParseResult.GetValue(MetricUrlsOption),
                    context.ParseResult.GetValue(ProvideMetricsOption),
                    context.ParseResult.GetValue(DiagnosticPortOption),
                    context.ParseResult.GetValue(NoAuthOption),
                    context.ParseResult.GetValue(TempApiKeyOption),
                    context.ParseResult.GetValue(NoHttpEgressOption),
                    context.ParseResult.GetValue(ConfigurationFilePathOption),
                    context.ParseResult.GetValue(ConfigLevelOption),
                    context.ParseResult.GetValue(ShowSourcesOption));
            });

            Command configCommand = new Command(
                name: "config",
                description: Strings.HelpDescription_CommandConfig)
            {
                showCommand
            };

            return configCommand;
        }

        private static Option<string[]> UrlsOption =
            new Option<string[]>(
                aliases: new[] { "-u", "--urls" },
                description: Strings.HelpDescription_OptionUrls,
                defaultValueFactory: () => new[] { "https://localhost:52323" })
            {
                ArgumentHelpName = "urls"
            };

        private static Option<string[]> MetricUrlsOption =
            new Option<string[]>(
                aliases: new[] { "--metricUrls" },
                description: Strings.HelpDescription_OptionMetricsUrls,
                defaultValueFactory: () => new[] { "http://localhost:52325" })
            {
                ArgumentHelpName = "metricUrls"
            };

        private static Option<bool> ProvideMetricsOption =
            new Option<bool>(
                aliases: new[] { "-m", "--metrics" },
                description: Strings.HelpDescription_OptionMetrics,
                defaultValueFactory: () => true)
            {
                ArgumentHelpName = "metrics"
            };

        private static Option<string> DiagnosticPortOption =
            new Option<string>(
                name: "--diagnostic-port",
                description: Strings.HelpDescription_OptionDiagnosticPort)
            {
                ArgumentHelpName = "diagnosticPort"
            };

        private static Option<FileInfo> ConfigurationFilePathOption =
            new Option<FileInfo>(
                name: "--configuration-file-path",
                description: Strings.HelpDescription_OptionConfigurationFilePath)
            {
                ArgumentHelpName = "configurationFilePath"
            };

        private static Option<bool> NoAuthOption =
            new Option<bool>(
                name: "--no-auth",
                description: Strings.HelpDescription_OptionNoAuth,
                defaultValueFactory: () => false)
            {
                ArgumentHelpName = "noAuth"
            };

        private static Option<bool> NoHttpEgressOption =
            new Option<bool>(
                name: "--no-http-egress",
                description: Strings.HelpDescription_OptionNoHttpEgress,
                defaultValueFactory: () => false)
            {
                ArgumentHelpName = "noHttpEgress"
            };

        private static Option<bool> TempApiKeyOption =
            new Option<bool>(
                name: "--temp-apikey",
                description: Strings.HelpDescription_OptionTempApiKey,
                defaultValueFactory: () => false)
            {
                ArgumentHelpName = "tempApiKey"
            };

        private static Option<OutputFormat> OutputOption =
            new Option<OutputFormat>(
                aliases: new[] { "-o", "--output" },
                description: Strings.HelpDescription_OutputFormat,
                defaultValueFactory: () => OutputFormat.Json)
            {
                ArgumentHelpName = "output"
            };

        private static Option<ConfigDisplayLevel> ConfigLevelOption =
            new Option<ConfigDisplayLevel>(
                name: "--level",
                description: Strings.HelpDescription_OptionLevel,
                defaultValueFactory: () => ConfigDisplayLevel.Redacted)
            {
                ArgumentHelpName = "level"
            };

        private static Option<bool> ShowSourcesOption =
            new Option<bool>(
                name: "--show-sources",
                description: Strings.HelpDescription_OptionShowSources,
                defaultValueFactory: () => false)
            {
                ArgumentHelpName = "showSources"
            };

        public static Task<int> Main(string[] args)
        {
            // Prevent child processes from inheriting startup hooks
            Environment.SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", null);

            TestAssemblies.SimulateStartupHook();

            var parser = new CommandLineBuilder(new RootCommand()
            {
                CollectCommand(),
                ConfigCommand(),
                GenerateApiKeyCommand()
            })
            .UseDefaults()
            .Build();

            return parser.InvokeAsync(args);
        }
    }

    internal enum ConfigDisplayLevel
    {
        Redacted,
        Full,
    }
}
