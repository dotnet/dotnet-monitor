// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal static class AppCommands
    {
        public static Task<bool> TryProcessAppCommand(string potentialCommand, ILogger logger)
        {
            switch (potentialCommand)
            {
                case TestAppScenarios.Commands.PrintEnvironmentVariables:
                    LogEnvironmentVariables(logger);
                    return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private static void LogEnvironmentVariables(ILogger logger)
        {
            // Only log specific variables required for testing instead of logging all environment variables
            LogEnvironmentVariableIfExist(logger, ProfilerIdentifiers.MutatingProfiler.EnvironmentVariables.ProductVersion);
            LogEnvironmentVariableIfExist(logger, ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion);
            LogEnvironmentVariableIfExist(logger, TestAppScenarios.EnvironmentVariables.CustomVariableName);
            LogEnvironmentVariableIfExist(logger, TestAppScenarios.EnvironmentVariables.IncrementVariableName);
        }

        private static void LogEnvironmentVariableIfExist(ILogger logger, string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value))
            {
                logger.EnvironmentVariable(name, value);
            }
        }
    }
}
