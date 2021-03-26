﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal static class ScenarioHelpers
    {
        public static async Task<int> RunScenarioAsync(Func<ILogger, Task<int>> func, CancellationToken token)
        {
            // Create JSON console logger so that app can communicate with test host
            // with structured responses.
            using ServiceProvider hostServices = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddFilter(typeof(Program).FullName, LogLevel.Debug)
                        .AddJsonConsole(options =>
                        {
                            options.UseUtcTimestamp = true;
                        });
                }).BuildServiceProvider();

            // All test host communication should be sent through this logger.
            ILogger<Program> logger = hostServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger<Program>();

            logger.ScenarioState(TestAppScenarios.SenarioState.Ready);

            // Wait for test host before executing scenario
            await WaitForCommandAsync(TestAppScenarios.Commands.StartScenario, logger);

            logger.ScenarioState(TestAppScenarios.SenarioState.Executing);

            int result = -1;
            try
            {
                result = await func(logger);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception: {ex}");
            }

            logger.ScenarioState(TestAppScenarios.SenarioState.Finished);

            // Wait for test host before ending scenario
            await WaitForCommandAsync(TestAppScenarios.Commands.EndScenario, logger);

            return result;
        }

        public static async Task WaitForCommandAsync(string expectedCommand, ILogger logger)
        {
            logger.ScenarioState(TestAppScenarios.SenarioState.Waiting);

            bool receivedExpected = false;
            string line;

            while (!receivedExpected && null != (line = await Console.In.ReadLineAsync()))
            {
                receivedExpected = string.Equals(expectedCommand, line, StringComparison.Ordinal);

                logger.ReceivedCommand(line, receivedExpected);
            }
        }
    }
}
