// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal static class TestTimeouts
    {
        /// <summary>
        /// Default timeout for HTTP API calls
        /// </summary>
        public static readonly TimeSpan HttpApi = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Default logs collection duration.
        /// </summary>
        public static readonly TimeSpan LogsDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Timeout for polling a long running operation to completion.
        /// This may need to be adjusted for individual calls that are longer than 30 seconds.
        /// </summary>
        public static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);
    }
}
