// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Async waits until it receives the Continue command.
    /// </summary>
    internal static class AsyncWaitScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.AsyncWait.Name);
            command.SetAction(ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue, logger);

                return 0;
            }, token);
        }
    }
}
