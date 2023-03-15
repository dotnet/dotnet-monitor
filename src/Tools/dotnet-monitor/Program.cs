﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Commands;
using System;
using System.CommandLine;
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

            command.SetHandler((context, token) =>
            {
                return GenerateApiKeyCommandHandler.Invoke(
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

            command.SetHandler((context, token) =>
            {
                return CollectCommandHandler.Invoke(
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
                return 0;
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
            new Option<string[]>("--urls", "-u")
            {
                DefaultValueFactory = (_) => new[] { "https://localhost:52323" },
                Description = Strings.HelpDescription_OptionUrls,
                HelpName = "urls"
            };

        private static Option<string[]> MetricUrlsOption =
            new Option<string[]>("--metricUrls")
            {
                DefaultValueFactory = (_) => new[] { "http://localhost:52325" },
                Description = Strings.HelpDescription_OptionMetricsUrls,
                HelpName = "metricUrls"
            };

        private static Option<bool> ProvideMetricsOption =
            new Option<bool>("--metrics", "-m")
            {
                DefaultValueFactory = (_) => true,
                Description = Strings.HelpDescription_OptionMetrics,
                HelpName = "metrics"
            };

        private static Option<string> DiagnosticPortOption =
            new Option<string>("--diagnostic-port")
            {
                Description = Strings.HelpDescription_OptionDiagnosticPort,
                HelpName = "diagnosticPort"
            };

        private static Option<FileInfo> ConfigurationFilePathOption =
            new Option<FileInfo>("--configuration-file-path")
            {
                Description = Strings.HelpDescription_OptionConfigurationFilePath,
                HelpName = "configurationFilePath"
            };

        private static Option<bool> NoAuthOption =
            new Option<bool>("--no-auth")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionNoAuth,
                HelpName = "noAuth"
            };

        private static Option<bool> NoHttpEgressOption =
            new Option<bool>("--no-http-egress")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionNoHttpEgress,
                HelpName = "noHttpEgress"
            };

        private static Option<bool> TempApiKeyOption =
            new Option<bool>("--temp-apikey")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionTempApiKey,
                HelpName = "tempApiKey"
            };

        private static Option<OutputFormat> OutputOption =
            new Option<OutputFormat>("--output", "-o")
            {
                DefaultValueFactory = (_) => OutputFormat.Json,
                Description = Strings.HelpDescription_OutputFormat,
                HelpName = "output"
            };

        private static Option<ConfigDisplayLevel> ConfigLevelOption =
            new Option<ConfigDisplayLevel>("--level")
            {
                DefaultValueFactory = (_) => ConfigDisplayLevel.Redacted,
                Description = Strings.HelpDescription_OptionLevel,
                HelpName = "level"
            };

        private static Option<bool> ShowSourcesOption =
            new Option<bool>("--show-sources")
            {
                DefaultValueFactory = (_) => false,
                Description = Strings.HelpDescription_OptionShowSources,
                HelpName = "showSources"
            };

        public static Task<int> Main(string[] args)
        {
            // Prevent child processes from inheriting startup hooks
            Environment.SetEnvironmentVariable(ToolIdentifiers.EnvironmentVariables.StartupHooks, null);

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
