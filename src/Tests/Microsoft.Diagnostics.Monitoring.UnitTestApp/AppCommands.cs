// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal static class AppCommands
    {
        public static async Task<bool> TryProcessAppCommand(string potentialCommand, ILogger logger)
        {
            switch (potentialCommand)
            {
                case TestAppScenarios.Commands.PrintEnvironmentVariables:
                    await PrintEnvironmentVariables(logger);
                    return true;
            }
            return false;
        }

        private static Task PrintEnvironmentVariables(ILogger logger)
        {
            IDictionary vars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in vars)
            {
                logger.EnvironmentVariable((string)entry.Key, (string)entry.Value);
            }
            return Task.CompletedTask;
        }
    }
}
