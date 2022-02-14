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
                description: Strings.HelpDescription_OptionUrls)
            {
                Argument = new Argument<string[]>(name: "urls", getDefaultValue: () => new[] { "https://localhost:52323" })
            };

        private static Option MetricUrls() =>
            new Option(
                aliases: new[] { "--metricUrls" },
                description: Strings.HelpDescription_OptionMetricsUrls)
            {
                Argument = new Argument<string[]>(name: "metricUrls", getDefaultValue: () => new[] { "http://localhost:52325" })
            };

        private static Option ProvideMetrics() =>
            new Option(
                aliases: new[] { "-m", "--metrics" },
                description: Strings.HelpDescription_OptionMetrics)
            {
                Argument = new Argument<bool>(name: "metrics", getDefaultValue: () => true)
            };

        private static Option DiagnosticPort() =>
            new Option(
                alias: "--diagnostic-port",
                description: Strings.HelpDescription_OptionDiagnosticPort)
            {
                Argument = new Argument<string>(name: "diagnosticPort")
            };

        private static Option NoAuth() =>
            new Option(
                alias: "--no-auth",
                description: Strings.HelpDescription_OptionNoAuth
                )
            {
                Argument = new Argument<bool>(name: "noAuth", getDefaultValue: () => false)
            };

        private static Option NoHttpEgress() =>
            new Option(
                alias: "--no-http-egress",
                description: Strings.HelpDescription_OptionNoHttpEgress
                )
            {
                Argument = new Argument<bool>(name: "noHttpEgress", getDefaultValue: () => false)
            };

        private static Option TempApiKey() =>
            new Option(
                alias: "--temp-apikey",
                description: Strings.HelpDescription_OptionTempApiKey
                )
            {
                Argument = new Argument<bool>(name: "tempApiKey", getDefaultValue: () => false)
            };

        private static Option Output() =>
            new Option(
                aliases: new[] { "-o", "--output" },
                description: Strings.HelpDescription_OutputFormat)
            {
                Argument = new Argument<OutputFormat>(name: "output", getDefaultValue: () => OutputFormat.Json)
            };

        private static Option ConfigLevel() =>
            new Option(
                alias: "--level",
                description: Strings.HelpDescription_OptionLevel)
            {
                Argument = new Argument<ConfigDisplayLevel>(name: "level", getDefaultValue: () => ConfigDisplayLevel.Redacted)
            };

        private static Option ShowSources() =>
            new Option(
                alias: "--show-sources",
                description: Strings.HelpDescription_OptionShowSources)
            {
                Argument = new Argument<bool>(name: "showSources", getDefaultValue: () => false)
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
