// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Async waits until it receives the Continue command.
    /// </summary>
    internal static class ExecuteScenario
    {
        private static readonly CliArgument<string> ContentArgument = new CliArgument<string>("content");

        private static readonly CliArgument<int> DelayArgument = new CliArgument<int>("delay");

        private static readonly CliArgument<FileInfo> PathArgument = new CliArgument<FileInfo>("path");

        public static CliCommand Command()
        {
            CliCommand nonZeroExitCodeCommand = new CliCommand(ActionTestsConstants.NonZeroExitCode);
            nonZeroExitCodeCommand.SetAction(Execute_NonZeroExitCode);

            CliCommand sleepCommand = new CliCommand(ActionTestsConstants.Sleep);
            sleepCommand.Arguments.Add(DelayArgument);
            sleepCommand.SetAction(Execute_Sleep);

            CliCommand textFileOutputCommand = new CliCommand(ActionTestsConstants.TextFileOutput);
            textFileOutputCommand.Arguments.Add(PathArgument);
            textFileOutputCommand.Arguments.Add(ContentArgument);
            textFileOutputCommand.SetAction(Execute_TextFileOutput);

            CliCommand zeroExitCodeCommand = new CliCommand(ActionTestsConstants.ZeroExitCode);
            zeroExitCodeCommand.SetAction(Execute_ZeroExitCode);

            CliCommand command = new(TestAppScenarios.Execute.Name);
            command.Subcommands.Add(nonZeroExitCodeCommand);
            command.Subcommands.Add(sleepCommand);
            command.Subcommands.Add(textFileOutputCommand);
            command.Subcommands.Add(zeroExitCodeCommand);
            return command;
        }

        private static Task<int> Execute_ZeroExitCode(ParseResult result, CancellationToken token)
        {
            return Task.FromResult(0);
        }

        private static Task<int> Execute_NonZeroExitCode(ParseResult result, CancellationToken token)
        {
            return Task.FromResult(1);
        }

        private static void Execute_Sleep(ParseResult result)
        {
            Thread.Sleep(result.GetValue(DelayArgument));
        }

        private static void Execute_TextFileOutput(ParseResult result)
        {
            File.WriteAllText(result.GetValue(PathArgument).FullName, result.GetValue(ContentArgument));
        }
    }
}
