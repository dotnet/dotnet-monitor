// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class ExecuteScenario
    {
        public static Command Command()
        {
            Command nonZeroExitCodeCommand = new Command(ActionTestsConstants.NonZeroExitCode);
            nonZeroExitCodeCommand.SetHandler(Execute_NonZeroExitCode);

            Command sleepCommand = new Command(ActionTestsConstants.Sleep);
            Argument<int> delayArgument = new Argument<int>("delay");
            sleepCommand.Arguments.Add(delayArgument);
            sleepCommand.SetHandler(Execute_Sleep, delayArgument);

            Command textFileOutputCommand = new Command(ActionTestsConstants.TextFileOutput);
            Argument<FileInfo> pathArgument = new Argument<FileInfo>("path");
            textFileOutputCommand.Arguments.Add(pathArgument);
            Argument<string> contentArgument = new Argument<string>("content");
            textFileOutputCommand.Arguments.Add(contentArgument);
            textFileOutputCommand.SetHandler(Execute_TextFileOutput, pathArgument, contentArgument);

            Command zeroExitCodeCommand = new Command(ActionTestsConstants.ZeroExitCode);
            zeroExitCodeCommand.SetHandler(Execute_ZeroExitCode);

            Command command = new(TestAppScenarios.Execute.Name);
            command.Subcommands.Add(nonZeroExitCodeCommand);
            command.Subcommands.Add(sleepCommand);
            command.Subcommands.Add(textFileOutputCommand);
            command.Subcommands.Add(zeroExitCodeCommand);
            return command;
        }

        private static void Execute_ZeroExitCode(InvocationContext context)
        {
            context.ExitCode = 0;
        }

        private static void Execute_NonZeroExitCode(InvocationContext context)
        {
            context.ExitCode = 1;
        }

        private static void Execute_Sleep(int delay)
        {
            Thread.Sleep(delay);
        }

        private static void Execute_TextFileOutput(FileInfo file, string content)
        {
            File.WriteAllText(file.FullName, content);
        }
    }
}
