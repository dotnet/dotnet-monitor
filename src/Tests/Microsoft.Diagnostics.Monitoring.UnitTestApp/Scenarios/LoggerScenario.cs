// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal class LoggerScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.Logger.Name);
            command.Handler = CommandHandler.Create((Func<CancellationToken, Task<int>>)ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
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
            }, token);
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
            logger.LogInformation(new EventId(1, "EventIdInformation"), "Information message with values {value1} and {value2}.", "hello", "goodbye");
        }

        private static void LogWarningMessage(ILogger logger)
        {
            logger.LogWarning(new EventId(1, "EventIdWarning"), "Warning message with values {value1} and {value2}.", 3.5d, 7L);
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
    }
}
