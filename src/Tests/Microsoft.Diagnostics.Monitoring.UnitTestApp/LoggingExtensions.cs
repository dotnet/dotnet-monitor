// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, TestAppScenarios.SenarioState, Exception> _scenarioState =
            LoggerMessage.Define<TestAppScenarios.SenarioState>(
                eventId: new EventId(1, "ScenarioState"),
                logLevel: LogLevel.Information,
                formatString: "State: {state}");

        private static readonly Action<ILogger, string, bool, Exception> _receivedCommand =
            LoggerMessage.Define<string, bool>(
                eventId: new EventId(2, "ReceivedCommand"),
                logLevel: LogLevel.Debug,
                formatString: "Received command: {command}; Expected: {expected}");

        public static void ScenarioState(this ILogger logger, TestAppScenarios.SenarioState state)
        {
            _scenarioState(logger, state, null);
        }

        public static void ReceivedCommand(this ILogger logger, string command, bool expected)
        {
            _receivedCommand(logger, command, expected, null);
        }
    }
}
