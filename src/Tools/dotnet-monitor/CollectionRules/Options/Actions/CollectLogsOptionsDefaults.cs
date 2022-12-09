// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal static class CollectLogsOptionsDefaults
    {
        public const Extensions.Logging.LogLevel DefaultLevel = Extensions.Logging.LogLevel.Warning;
        public const bool UseAppFilters = true;
        public const string Duration = "00:00:30";
        public const LogFormat Format = LogFormat.PlainText;
    }
}
