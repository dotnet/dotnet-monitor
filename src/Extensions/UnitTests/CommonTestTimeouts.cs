// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace UnitTests
{
    public static class CommonTestTimeouts
    {
        /// <summary>
        /// Default timeout for waiting for Azurite to fully initialize.
        /// </summary>
        public static readonly TimeSpan AzuriteInitializationTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Default timeout for waiting for Azurite to fully stop.
        /// </summary>
        public static readonly TimeSpan AzuriteTeardownTimeout = TimeSpan.FromSeconds(30);
    }
}
