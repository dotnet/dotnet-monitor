// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class TestTimeouts
    {
        /// <summary>
        /// Timeout for an egress unit test (Must be const int to be used as an attribute).
        /// </summary>
        public const int EgressUnitTestTimeoutMs = 30 * 1000; // 30 seconds
    }
}
