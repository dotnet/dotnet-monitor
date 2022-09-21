﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                    builder.AddFilter(typeof(Program).FullName, LogLevel.Debug);
                    // Console logger infra is not writing out all lines during process lifetime
                    // which causes the coordination between the unit test and the test app to fail.
                    // Temporarily replace with custom JSON console logger.
                    //builder.AddJsonConsole(options =>
                    //{
                    //    options.UseUtcTimestamp = true;
                    //});
                    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, JsonConsoleLoggerProvider>());
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
            await WaitForCommandAsync(new string[] { expectedCommand }, logger);
        }

        public static async Task<string> WaitForCommandAsync(string[] expectedCommands, ILogger logger)
        {
            logger.ScenarioState(TestAppScenarios.SenarioState.Waiting);

            bool receivedExpected = false;
            string line, commandReceived = null;

            while (!receivedExpected && null != (line = await Console.In.ReadLineAsync()))
            {
                if (await AppCommands.TryProcessAppCommand(line, logger))
                {
                    // We successfully received an app-level command and processed it.
                    // Restart the loop and wait for the command the scenario is expecting.
                    receivedExpected = false;
                    // Still acknowledge this command and say it was expected
                    logger.ReceivedCommand(line, expected: true);
                    continue;
                }

                receivedExpected = expectedCommands.Contains(line, StringComparer.Ordinal);
                if (receivedExpected)
                {
                    commandReceived = line;
                }

                logger.ReceivedCommand(line, receivedExpected);
            }

            return commandReceived;
        }

        private class JsonConsoleLoggerProvider : ILoggerProvider
        {
            private readonly ConcurrentDictionary<string, JsonConsoleLogger> _loggers = new();

            public ILogger CreateLogger(string categoryName)
            {
                return _loggers.GetOrAdd(categoryName, name => new JsonConsoleLogger(name));
            }

            public void Dispose()
            {
            }

            private class JsonConsoleLogger : ILogger
            {
                private string _categoryName;

                public JsonConsoleLogger(string categoryName)
                {
                    _categoryName = categoryName;
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    using Utf8JsonWriter writer = new(Console.OpenStandardOutput());

                    writer.WriteStartObject();

                    writer.WriteNumber("EventId", eventId.Id);
                    writer.WriteString("Level", logLevel.ToString("G"));
                    writer.WriteString("Category", _categoryName);
                    writer.WriteString("Message", formatter(state, exception));

                    if (null != state)
                    {
                        writer.WriteStartObject("State");
                        writer.WriteString("Message", state.ToString());

                        if ((object)state is IReadOnlyCollection<KeyValuePair<string, object>> readOnlyCollection)
                        {
                            foreach (KeyValuePair<string, object> item in readOnlyCollection)
                            {
                                if (item.Value is string stringValue)
                                {
                                    writer.WriteString(item.Key, stringValue);
                                }
                                else if (item.Value is Enum enumValue)
                                {
                                    writer.WriteString(item.Key, enumValue.ToString("G"));
                                }
                                else if (item.Value is bool booleanValue)
                                {
                                    writer.WriteBoolean(item.Key, booleanValue);
                                }
                                else
                                {
                                    writer.WriteString(item.Key, "[UNHANDLED]");
                                }
                            }
                        }
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();

                    writer.Flush();

                    Console.WriteLine();
                }
            }
        }
    }
}
