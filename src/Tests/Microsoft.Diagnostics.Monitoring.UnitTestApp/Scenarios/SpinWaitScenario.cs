﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Synchronously spins until it receives the Continue command.
    /// </summary>
    internal class SpinWaitScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.SpinWait.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin, logger);

                Task continueTask = Task.Run(() => ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin, logger));

                while (!continueTask.IsCompleted)
                {
                    Thread.SpinWait(1_000_000);
                }

                return 0;
            }, context.GetCancellationToken());
        }
    }
}
