// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class CommonTestTimeouts
    {
        /// <summary>
        /// Default timeout for sending commands from the test to a process.
        /// </summary>
        public static readonly TimeSpan SendCommand = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Default timeout for starting an executable.
        /// </summary>
        public static readonly TimeSpan StartProcess = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Default timeout for waiting for an executable to exit.
        /// </summary>
        public static readonly TimeSpan WaitForExit = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Default timeout for gcdump collection.
        /// </summary>
        /// <remarks>
        /// GCDumps can be quite large and take a significant amount of time to transfer.
        /// </remarks>
        public static readonly TimeSpan GCDumpTimeout = TimeSpan.FromMinutes(3);
    }
}
