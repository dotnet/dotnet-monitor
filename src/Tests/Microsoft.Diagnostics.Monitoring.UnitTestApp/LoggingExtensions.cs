// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _scenarioReady =
            LoggerMessage.Define(
                eventId: new EventId(1, "ScenarioReady"),
                logLevel: LogLevel.Information,
                formatString: "Scenario ready.");

        private static readonly Action<ILogger, Exception> _scenarioFinished =
            LoggerMessage.Define(
                eventId: new EventId(2, "ScenarioFinished"),
                logLevel: LogLevel.Information,
                formatString: "Scenario finished.");

        private static readonly Action<ILogger, Exception> _scenarioExecuting =
            LoggerMessage.Define(
                eventId: new EventId(3, "ScenarioExecuting"),
                logLevel: LogLevel.Information,
                formatString: "Scenario executing.");

        private static readonly Action<ILogger, Exception> _waitTestHost =
            LoggerMessage.Define(
                eventId: new EventId(4, "WaitTestHost"),
                logLevel: LogLevel.Information,
                formatString: "Waiting for test host.");

        private static readonly Action<ILogger, string, bool, Exception> _receivedCommand =
            LoggerMessage.Define<string, bool>(
                eventId: new EventId(5, "ReceivedCommand"),
                logLevel: LogLevel.Debug,
                formatString: "Received command: {command}; Expected: {expected}");

        public static void ScenarioReady(this ILogger logger)
        {
            _scenarioReady(logger, null);
        }

        public static void ScenarioFinished(this ILogger logger)
        {
            _scenarioFinished(logger, null);
        }

        public static void ScenarioExecuting(this ILogger logger)
        {
            _scenarioExecuting(logger, null);
        }

        public static void WaitTestHost(this ILogger logger)
        {
            _waitTestHost(logger, null);
        }

        public static void ReceivedCommand(this ILogger logger, string command, bool expected)
        {
            _receivedCommand(logger, command, expected, null);
        }
    }
}
