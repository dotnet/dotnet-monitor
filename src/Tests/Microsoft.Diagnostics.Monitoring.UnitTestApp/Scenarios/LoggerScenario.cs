﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal class LoggerScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.Logger.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                using ServiceProvider services = new ServiceCollection()
                    .AddLogging(builder =>
                    {
                        builder.AddEventSourceLogger();
                        builder.AddFilter(null, LogLevel.None); // Default
                        builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory1, LogLevel.Debug);
                        builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory2, LogLevel.Information);
                        builder.AddFilter(TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Warning);
                    }).BuildServiceProvider();

                ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Logger.Commands.StartLogging, logger);

                ILogger cat1Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory1);
                LogTraceMessage(cat1Logger);
                LogDebugMessage(cat1Logger);
                LogInformationMessage(cat1Logger);
                LogWarningMessage(cat1Logger);
                LogErrorMessage(cat1Logger);
                LogCriticalMessage(cat1Logger);

                ILogger cat2Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory2);
                LogTraceMessage(cat2Logger);
                LogDebugMessage(cat2Logger);
                LogInformationMessage(cat2Logger);
                LogWarningMessage(cat2Logger);
                LogErrorMessage(cat2Logger);
                LogCriticalMessage(cat2Logger);

                ILogger cat3Logger = loggerFactory.CreateLogger(TestAppScenarios.Logger.Categories.LoggerCategory3);
                LogTraceMessage(cat3Logger);
                LogDebugMessage(cat3Logger);
                LogInformationMessage(cat3Logger);
                LogWarningMessage(cat3Logger);
                LogErrorMessage(cat3Logger);
                LogCriticalMessage(cat3Logger);

                return 0;
            }, context.GetCancellationToken());
        }

        private static void LogTraceMessage(ILogger logger)
        {
            logger.LogTrace(new EventId(1, "EventIdTrace"), "Trace message with values {value1} and {value2}.", 3, true);
        }

        private static void LogDebugMessage(ILogger logger)
        {
            logger.LogDebug(new EventId(1, "EventIdDebug"), "Debug message with values {value1} and {value2}.", new Guid("F39A5065-732B-4CCE-89D1-52E4AF39E233"), null);
        }

        private static void LogInformationMessage(ILogger logger)
        {
            logger.LogInformation("Information message with values {value1} and {value2}.", "hello", "goodbye");
        }

        private static void LogWarningMessage(ILogger logger)
        {
            logger.Log(
                LogLevel.Warning,
                new EventId(5, "EventIdWarning"),
                new CustomLogState(
                    "Warning message with custom state.",
                    new string[] { "KeyA", "Key2", "KeyZ" },
                    new object[] { 4, 'p', LogLevel.Error }),
                null,
                CustomLogState.Formatter);
        }

        private static void LogErrorMessage(ILogger logger)
        {
            logger.LogError(new EventId(1, "EventIdError"), "Error message with values {value1} and {value2}.", 'a', new IntPtr(42));
        }

        private static void LogCriticalMessage(ILogger logger)
        {
            try
            {
                throw new InvalidOperationException("Application is shutting down.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogCritical(new EventId(1, "EventIdCritical"), ex, "Critical message.");
            }
        }

        private readonly struct CustomLogState : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<CustomLogState, Exception, string> Formatter = (state, exception) => $"'{state._message}' with {state._keys.Length} state values.";

            private readonly string[] _keys;
            private readonly string _message;
            private readonly object[] _values;

            public CustomLogState(string message, string[] keys, object[] values)
            {
                _message = message;
                _keys = keys ?? throw new ArgumentNullException(nameof(keys));
                _values = values ?? throw new ArgumentNullException(nameof(values));

                if (_keys.Length != _values.Length)
                {
                    throw new ArgumentException($"{nameof(keys)} and {nameof(values)} must have the same length.");
                }
            }

            public KeyValuePair<string, object> this[int index] => new KeyValuePair<string, object>(_keys[index], _values[index]);

            public int Count => _keys.Length;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                return _message;
            }
        }
    }
}
