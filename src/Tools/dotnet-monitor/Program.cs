// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Tools.Monitor.Commands;
using Microsoft.Tools.Common;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    class Program
    {
        private static Command GenerateApiKeyCommand() =>
            new Command(
                name: "generatekey",
                description: Strings.HelpDescription_CommandGenerateKey)
            {
                CommandHandler.Create(GenerateApiKeyCommandHandler.Invoke),
                Output()
            };

        private static Command CollectCommand() =>
            new Command(
                name: "collect",
                description: Strings.HelpDescription_CommandCollect)
            {
                // Handler
                CommandHandler.Create(CollectCommandHandler.Invoke),
                SharedOptions()
            };

        private static Command ConfigCommand() =>
            new Command(
                name: "config",
                description: Strings.HelpDescription_CommandConfig)
            {
                new Command(
                    name: "show",
                    description: Strings.HelpDescription_CommandShow)
                {
                    // Handler
                    CommandHandler.Create(ConfigShowCommandHandler.Invoke),
                    SharedOptions(),
                    ConfigLevel(),
                    ShowSources()
                }
            };

        private static IEnumerable<Option> SharedOptions() => new Option[]
        {
            Urls(), MetricUrls(), ProvideMetrics(), DiagnosticPort(), NoAuth(), TempApiKey(), NoHttpEgress()
        };
        
        private static Option Urls() =>
            new Option(
                aliases: new[] { "-u", "--urls" },
                description: Strings.HelpDescription_OptionUrls,
                getDefaultValue: () => new[] { "https://localhost:52323" })
            {
                ArgumentHelpName = "urls"
            };

        private static Option MetricUrls() =>
            new Option<string[]>(
                aliases: new[] { "--metricUrls" },
                description: Strings.HelpDescription_OptionMetricsUrls,
                getDefaultValue: () => new[] { "http://localhost:52325" })
            {
                ArgumentHelpName = "metricUrls"
            };

        private static Option ProvideMetrics() =>
            new Option(
                aliases: new[] { "-m", "--metrics" },
                description: Strings.HelpDescription_OptionMetrics,
                getDefaultValue: () => true)
            {
                ArgumentHelpName = "metrics"
            };

        private static Option DiagnosticPort() =>
            new Option(
                alias: "--diagnostic-port",
                description: Strings.HelpDescription_OptionDiagnosticPort)
            {
                ArgumentHelpName = "diagnosticPort"
            };

        private static Option NoAuth() =>
            new Option(
                alias: "--no-auth",
                description: Strings.HelpDescription_OptionNoAuth,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "noAuth"
            };

        private static Option NoHttpEgress() =>
            new Option(
                alias: "--no-http-egress",
                description: Strings.HelpDescription_OptionNoHttpEgress,
                getDefaultValue: () => false)
            {
                ArgumentHelpName =  "noHttpEgress"
            };

        private static Option TempApiKey() =>
            new Option(
                alias: "--temp-apikey",
                description: Strings.HelpDescription_OptionTempApiKey,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "tempApiKey"
            };

        private static Option Output() =>
            new Option(
                aliases: new[] { "-o", "--output" },
                description: Strings.HelpDescription_OutputFormat,
                getDefaultValue: () => OutputFormat.Json)
            {
                ArgumentHelpName = "output"
            };

        private static Option ConfigLevel() =>
            new Option(
                alias: "--level",
                description: Strings.HelpDescription_OptionLevel,
                getDefaultValue: () => ConfigDisplayLevel.Redacted)
            {
                ArgumentHelpName = "level"
            };

        private static Option ShowSources() =>
            new Option(
                alias: "--show-sources",
                description: Strings.HelpDescription_OptionShowSources,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "showSources"
            };

        public static Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder()
                .AddCommand(CollectCommand())
                .AddCommand(ConfigCommand())
                .AddCommand(GenerateApiKeyCommand())
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
