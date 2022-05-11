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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    class Program
    {
        private static Command GenerateApiKeyCommand()
        {
            Command command = new Command(
                name: "generatekey",
                description: Strings.HelpDescription_CommandGenerateKey);

            var output = Output();

            command.Add(output);

            command.SetHandler((CancellationToken token, OutputFormat output, IConsole console) => GenerateApiKeyCommandHandler.Invoke(token, output, console), output);

            return command;
        }

        private static Command CollectCommand()
        {
            var urls = Urls();
            var metricUrls = MetricUrls();
            var provideMetrics = ProvideMetrics();
            var diagnosticPort = DiagnosticPort();
            var noAuth = NoAuth();
            var tempApiKey = TempApiKey();
            var noHttpEgress = NoHttpEgress();

            Command command = new Command(
                name: "collect",
                description: Strings.HelpDescription_CommandCollect)
            {
                urls, metricUrls, provideMetrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress
            };

            //command.Add(SharedOptions());

            //             Urls(), MetricUrls(), ProvideMetrics(), DiagnosticPort(), NoAuth(), TempApiKey(), NoHttpEgress()

            command.SetHandler((CancellationToken token, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress) => CollectCommandHandler.Invoke(token, urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress), urls, metricUrls, provideMetrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress);

            return command;
        }

        /*
        private static Command CollectCommand() =>
            new Command(
                name: "collect",
                description: Strings.HelpDescription_CommandCollect)
            {
                // Handler
                CommandHandler.Create(CollectCommandHandler.Dummy),
                SharedOptions()
            };
        */

        private static Command ConfigCommand()
        {
            // Doesn't have show
            Command command = new Command(
                name: "config",
                description: Strings.HelpDescription_CommandConfig);

            command.Add(SharedOptions());
            command.Add(ConfigLevel());
            command.Add(ShowSources());

            command.SetHandler((string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, ConfigDisplayLevel level, bool showSources) => ConfigShowCommandHandler.Invoke(urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress, level, showSources));

            return command;
        }

        private static IEnumerable<Option> SharedOptions() => new Option[]
        {
            Urls(), MetricUrls(), ProvideMetrics(), DiagnosticPort(), NoAuth(), TempApiKey(), NoHttpEgress()
        };
        
        private static Option Urls() =>
            new Option<string[]>(
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
            new Option<bool>(
                aliases: new[] { "-m", "--metrics" },
                description: Strings.HelpDescription_OptionMetrics,
                getDefaultValue: () => true)
            {
                ArgumentHelpName = "metrics"
            };

        private static Option DiagnosticPort() =>
            new Option<string>(
                name: "--diagnostic-port",
                description: Strings.HelpDescription_OptionDiagnosticPort)
            {
                ArgumentHelpName = "diagnosticPort"
            };

        private static Option NoAuth() =>
            new Option<bool>(
                name: "--no-auth",
                description: Strings.HelpDescription_OptionNoAuth,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "noAuth"
            };

        private static Option NoHttpEgress() =>
            new Option<bool>(
                name: "--no-http-egress",
                description: Strings.HelpDescription_OptionNoHttpEgress,
                getDefaultValue: () => false)
            {
                ArgumentHelpName =  "noHttpEgress"
            };

        private static Option TempApiKey() =>
            new Option<bool>(
                name: "--temp-apikey",
                description: Strings.HelpDescription_OptionTempApiKey,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "tempApiKey"
            };

        private static Option Output() =>
            new Option<OutputFormat>(
                aliases: new[] { "-o", "--output" },
                description: Strings.HelpDescription_OutputFormat,
                getDefaultValue: () => OutputFormat.Json)
            {
                ArgumentHelpName = "output"
            };

        private static Option ConfigLevel() =>
            new Option<ConfigDisplayLevel>(
                name: "--level",
                description: Strings.HelpDescription_OptionLevel,
                getDefaultValue: () => ConfigDisplayLevel.Redacted)
            {
                ArgumentHelpName = "level"
            };

        private static Option ShowSources() =>
            new Option<bool>(
                name: "--show-sources",
                description: Strings.HelpDescription_OptionShowSources,
                getDefaultValue: () => false)
            {
                ArgumentHelpName = "showSources"
            };

        public static Task<int> Main(string[] args)
        {
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
