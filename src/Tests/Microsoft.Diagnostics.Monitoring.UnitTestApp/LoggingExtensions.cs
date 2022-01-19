﻿// Licensed to the .NET Foundation under one or more agreements.
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
                eventId: TestAppLogEventIds.ScenarioState.EventId(),
                logLevel: LogLevel.Information,
                formatString: "State: {state}");

        private static readonly Action<ILogger, string, bool, Exception> _receivedCommand =
            LoggerMessage.Define<string, bool>(
                eventId: TestAppLogEventIds.ReceivedCommand.EventId(),
                logLevel: LogLevel.Debug,
                formatString: "Received command: {command}; Expected: {expected}");

        private static readonly Action<ILogger, string, string, Exception> _environmentVariable =
            LoggerMessage.Define<string, string>(
                eventId: TestAppLogEventIds.EnvironmentVariable.EventId(),
                logLevel: LogLevel.Information,
                formatString: "Environment Variable: {name} = {value}");

        public static void ScenarioState(this ILogger logger, TestAppScenarios.SenarioState state)
        {
            _scenarioState(logger, state, null);
        }

        public static void ReceivedCommand(this ILogger logger, string command, bool expected)
        {
            _receivedCommand(logger, command, expected, null);
        }

        public static void EnvironmentVariable(this ILogger logger, string name, string value)
        {
            _environmentVariable(logger, name, value, null);
        }
    }
}
