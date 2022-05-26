// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Tools.Monitor.Commands;
using Microsoft.Tools.Common;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.CommandLine.Binding;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    class Program
    {
        private static Command GenerateApiKeyCommand()
        {
            Command command = new Command(
                name: "generatekey",
                description: Strings.HelpDescription_CommandGenerateKey);

            command.Add(Output());

            command.SetHandler<CancellationToken, OutputFormat, IConsole>(GenerateApiKeyCommandHandler.Invoke, command.Children.OfType<IValueDescriptor>().ToArray());

            return command;
        }

        private static Command CollectCommand()
        {
            Command command = new Command(
                name: "collect",
                description: Strings.HelpDescription_CommandCollect)
            {
                SharedOptions()
            };

            command.SetHandler<CancellationToken, string[], string[], bool, string, bool, bool, bool, string>(CollectCommandHandler.Invoke, command.Children.OfType<IValueDescriptor>().ToArray());
 
            return command;
        }

        private static Command ConfigCommand()
        {
            Command showCommand = new Command(
                name: "show",
                description: Strings.HelpDescription_CommandShow)
            {
                SharedOptions(),
                ConfigLevel(),
                ShowSources()
            };

            showCommand.SetHandler<string[], string[], bool, string, bool, bool, bool, string, ConfigDisplayLevel, bool>(ConfigShowCommandHandler.Invoke, showCommand.Children.OfType<IValueDescriptor>().ToArray());

            Command configCommand = new Command(
                name: "config",
                description: Strings.HelpDescription_CommandConfig)
            {
                showCommand
            };

            return configCommand;
        }

        private static IEnumerable<Option> SharedOptions() => new Option[]
        {
            Urls(), MetricUrls(), ProvideMetrics(), DiagnosticPort(), NoAuth(), TempApiKey(), NoHttpEgress(), ConfigurationFilePath()
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

        private static Option ConfigurationFilePath() =>
            new Option<string>(
                name: "--configuration-file-path",
                description: Strings.HelpDescription_OptionConfigurationFilePath)
            {
                ArgumentHelpName = "configurationFilePath"
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
                ArgumentHelpName = "noHttpEgress"
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
