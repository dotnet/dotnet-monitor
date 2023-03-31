// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    internal static class Utilities
    {
        public static ILogger CreateLogger()
        {
            var logLevel = LogLevel.Information;

            if (Environment.GetEnvironmentVariables().Contains("LogLevel"))
            {
                logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), Environment.GetEnvironmentVariable("LogLevel"));
            }

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(logLevel);
            });

            return loggerFactory.CreateLogger<EgressHelper>();
        }
    }
}
