// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class StacksScenario
    {
        public static CliCommand Command()
        {
            CliCommand command = new(TestAppScenarios.Stacks.Name);

            command.SetAction(ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(ParseResult result, CancellationToken token)
        {
            using StacksWorker worker = new StacksWorker();

            //Background thread will create an expected callstack and pause.
            Thread thread = new Thread(Entrypoint);
            thread.Name = "TestThread";
            thread.Start(worker);

            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Stacks.Commands.Continue, logger);

                //Allow the background thread to resume work.
                worker.Signal();

                return 0;
            }, token);
        }

        public static void Entrypoint(object worker)
        {
            var stacksWorker = (StacksWorker)worker;
            stacksWorker.Work();
        }
    }
}
