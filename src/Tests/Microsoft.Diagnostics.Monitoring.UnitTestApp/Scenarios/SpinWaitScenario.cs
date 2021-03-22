// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Spin waits on thread to generate high CPU usage until it receives the Continue command.
    /// </summary>
    internal class SpinWaitScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.SpinWait.Name);
            command.Handler = CommandHandler.Create((Func<CancellationToken, Task<int>>)ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(CancellationToken token)
        {
            return Program.RunScenarioAsync(logger =>
            {
                Task commandTask = Program.WaitForCommandAsync(TestAppScenarios.SpinWait.Commands.Continue, logger);
                
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (commandTask.IsCompleted)
                    {
                        break;
                    }
                }

                return Task.FromResult(0);
            }, token);
        }
    }
}
