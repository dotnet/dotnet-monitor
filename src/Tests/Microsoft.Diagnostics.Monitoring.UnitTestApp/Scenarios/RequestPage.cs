// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Requests a page.
    /// </summary>
    internal class RequestPage
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
