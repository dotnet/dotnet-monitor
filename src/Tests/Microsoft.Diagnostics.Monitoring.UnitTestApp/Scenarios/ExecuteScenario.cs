// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.CommandLine.Invocation;
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
        private static readonly Argument<string> ContentArgument = new Argument<string>("content");

        private static readonly Argument<int> DelayArgument = new Argument<int>("delay");

        private static readonly Argument<FileInfo> PathArgument = new Argument<FileInfo>("path");

        public static Command Command()
        {
            Command nonZeroExitCodeCommand = new Command(ActionTestsConstants.NonZeroExitCode);
            nonZeroExitCodeCommand.SetActionWithExitCode(Execute_NonZeroExitCode);

            Command sleepCommand = new Command(ActionTestsConstants.Sleep);
            sleepCommand.Arguments.Add(DelayArgument);
            sleepCommand.SetAction(Execute_Sleep);

            Command textFileOutputCommand = new Command(ActionTestsConstants.TextFileOutput);
            textFileOutputCommand.Arguments.Add(PathArgument);
            textFileOutputCommand.Arguments.Add(ContentArgument);
            textFileOutputCommand.SetAction(Execute_TextFileOutput);

            Command zeroExitCodeCommand = new Command(ActionTestsConstants.ZeroExitCode);
            zeroExitCodeCommand.SetActionWithExitCode(Execute_ZeroExitCode);

            Command command = new(TestAppScenarios.Execute.Name);
            command.Subcommands.Add(nonZeroExitCodeCommand);
            command.Subcommands.Add(sleepCommand);
            command.Subcommands.Add(textFileOutputCommand);
            command.Subcommands.Add(zeroExitCodeCommand);
            return command;
        }

        private static Task<int> Execute_ZeroExitCode(InvocationContext context, CancellationToken token)
        {
            return Task.FromResult(0);
        }

        private static Task<int> Execute_NonZeroExitCode(InvocationContext context, CancellationToken token)
        {
            return Task.FromResult(1);
        }

        private static void Execute_Sleep(InvocationContext context)
        {
            Thread.Sleep(context.GetValue(DelayArgument));
        }

        private static void Execute_TextFileOutput(InvocationContext context)
        {
            File.WriteAllText(context.GetValue(PathArgument).FullName, context.GetValue(ContentArgument));
        }
    }
}
