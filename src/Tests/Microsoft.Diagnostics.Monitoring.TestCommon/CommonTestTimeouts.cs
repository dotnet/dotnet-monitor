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
        /// Default timeout for acquiring a trace.
        /// </summary>
        public static readonly TimeSpan TraceTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Default timeout for live metrics collection.
        /// </summary>
        public static readonly TimeSpan LiveMetricsTimeout = TimeSpan.FromMinutes(2);

        /// Default timeout for gcdump collection.
        /// </summary>
        /// <remarks>
        /// GCDumps can be quite large and take a significant amount of time to transfer.
        /// </remarks>
        public static readonly TimeSpan GCDumpTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Default timeout for dump collection.
        /// </summary>
        /// <remarks>
        /// Dumps (especially full dumps) can be quite large and take a significant amount of time to transfer.
        /// </remarks>
        public static readonly TimeSpan DumpTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Default timeout for logs collection.
        /// </summary>
        public static readonly TimeSpan LogsTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Default logs collection duration.
        /// </summary>
        public static readonly TimeSpan LogsDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Default timeout for environment variable manipulation.
        /// </summary>
        public static readonly TimeSpan EnvVarsTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Default timeout for loading a profiler into a target process.
        /// </summary>
        public static readonly TimeSpan LoadProfilerTimeout = TimeSpan.FromSeconds(10);

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
