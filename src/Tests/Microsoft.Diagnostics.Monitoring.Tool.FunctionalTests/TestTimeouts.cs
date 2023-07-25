// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class TestTimeouts
    {
        /// <summary>
        /// Timeout for polling a long running operation to completion.
        /// This may need to be adjusted for individual calls that are longer than 30 seconds.
        /// </summary>
        public static readonly TimeSpan OperationTimeout = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Timeout for metrics api.
        /// </summary>
        public static readonly TimeSpan CaptureMetricsTimeout = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Default timeout for HTTP API calls
        /// </summary>
        public static readonly TimeSpan HttpApi = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Timeout for waiting for a collection rule to complete.
        /// </summary>
        public static readonly TimeSpan CollectionRuleCompletionTimeout = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Timeout for waiting for a collection rule to be filtered.
        /// </summary>
        public static readonly TimeSpan CollectionRuleFilteredTimeout = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Timeout for waiting for a collection rule to run its action list to completion.
        /// </summary>
        public static readonly TimeSpan CollectionRuleActionsCompletedTimeout = CommonTestTimeouts.GeneralTimeout;

        /// <summary>
        /// Timeout for waiting for the dotnet-monitor process to exit after closing StandardInput.
        /// </summary>
        public static readonly TimeSpan DotnetMonitorExitAfterStdinCloseTimeout = TimeSpan.FromSeconds(10);
    }
}
