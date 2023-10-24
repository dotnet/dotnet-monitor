// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class CommonTestTimeouts
    {
        /// <summary>
        /// Default timeout for any task that doesn't need a more specific one.
        /// </summary>
        public static readonly TimeSpan GeneralTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Default timeout for sending commands from the test to a process.
        /// </summary>
        public static readonly TimeSpan SendCommand = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Default timeout for starting an executable.
        /// </summary>
        public static readonly TimeSpan StartProcess = GeneralTimeout;

        /// <summary>
        /// Default timeout for stopping an executable.
        /// </summary>
        public static readonly TimeSpan StopProcess = GeneralTimeout;

        /// <summary>
        /// Default timeout for waiting for an executable to exit.
        /// </summary>
        public static readonly TimeSpan WaitForExit = GeneralTimeout;

        /// <summary>
        /// Default timeout for acquiring a trace.
        /// </summary>
        public static readonly TimeSpan TraceTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for validating a trace.
        /// </summary>
        public static readonly TimeSpan ValidateTraceTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for live metrics collection.
        /// </summary>
        public static readonly TimeSpan LiveMetricsTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for gcdump collection.
        /// </summary>
        /// <remarks>
        /// GCDumps can be quite large and take a significant amount of time to transfer.
        /// </remarks>
        public static readonly TimeSpan GCDumpTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for dump collection.
        /// </summary>
        /// <remarks>
        /// Dumps (especially full dumps) can be quite large and take a significant amount of time to transfer.
        /// </remarks>
        public static readonly TimeSpan DumpTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for logs collection.
        /// </summary>
        public static readonly TimeSpan LogsTimeout = GeneralTimeout;

        /// <summary>
        /// Default logs collection duration.
        /// </summary>
        public static readonly TimeSpan LogsDuration = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Default live metrics collection duration.
        /// </summary>
        public static readonly int LiveMetricsDurationSeconds = 10;

        /// <summary>
        /// Default timeout for environment variable manipulation.
        /// </summary>
        public static readonly TimeSpan EnvVarsTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for loading a profiler into a target process.
        /// </summary>
        public static readonly TimeSpan LoadProfilerTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for waiting for Azurite to fully initialize.
        /// </summary>
        public static readonly TimeSpan AzuriteInitializationTimeout = GeneralTimeout;

        /// <summary>
        /// Default timeout for waiting for Azurite to fully stop.
        /// </summary>
        public static readonly TimeSpan AzuriteTeardownTimeout = GeneralTimeout;

        /// <summary>
        /// Amount of time to wait before sending batches of event source events in order to
        /// avoid real-time buffering issues in the runtime eventing infrastructure and the
        /// trace event library event processor.
        /// </summary>
        /// <remarks>
        /// See: https://github.com/dotnet/runtime/issues/76704
        /// </remarks>
        public static readonly TimeSpan EventSourceBufferAvoidanceTimeout = TimeSpan.FromMilliseconds(250);
    }
}
