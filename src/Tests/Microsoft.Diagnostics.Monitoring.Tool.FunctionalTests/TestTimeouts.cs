// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class TestTimeouts
    {
        /// <summary>
        /// Timeout for polling a long running operation to completion.
        /// This may need to be adjusted for individual calls that are longer than 30 seconds.
        /// </summary>
        public static readonly TimeSpan OperationTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Timeout for metrics api.
        /// </summary>
        public static readonly TimeSpan CaptureMetricsTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Default timeout for HTTP API calls
        /// </summary>
        public static readonly TimeSpan HttpApi = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Timeout for waiting for a collection rule to complete.
        /// </summary>
        public static readonly TimeSpan CollectionRuleCompletionTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Timeout for waiting for a collection rule to be filtered.
        /// </summary>
        public static readonly TimeSpan CollectionRuleFilteredTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Timeout for waiting for a collection rule to run its action list to completion.
        /// </summary>
        public static readonly TimeSpan CollectionRuleActionsCompletedTimeout = TimeSpan.FromMinutes(3);
    }
}
