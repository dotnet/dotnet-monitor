﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Prints out the environment variables.
    /// </summary>
    internal class EnvironmentVariablesScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.EnvironmentVariables.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            string[] acceptableCommands = new string[]
            {
                TestAppScenarios.EnvironmentVariables.Commands.IncVar,
                TestAppScenarios.EnvironmentVariables.Commands.ShutdownScenario,
            };
            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                while (true)
                {
                    string command = await ScenarioHelpers.WaitForCommandAsync(acceptableCommands, logger);

                    switch (command)
                    {
                        case TestAppScenarios.EnvironmentVariables.Commands.IncVar:
                            string currValue = Environment.GetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.IncrementVariableName) ?? "0";
                            if (!int.TryParse(currValue, out int oldValue))
                            {
                                oldValue = 0;
                            }
                            string newValue = (oldValue + 1).ToString();
                            Environment.SetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.IncrementVariableName, newValue);
                            break;
                        case TestAppScenarios.EnvironmentVariables.Commands.ShutdownScenario:
                            return 0;
                    }
                }
            }, context.GetCancellationToken());
        }
    }
}
