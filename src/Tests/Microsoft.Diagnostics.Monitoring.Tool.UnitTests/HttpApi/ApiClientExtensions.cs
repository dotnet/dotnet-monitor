// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
{
    internal static class ApiClientExtensions
    {
        /// <summary>
        /// GET /processes
        /// </summary>
        public static Task<IEnumerable<ProcessIdentifier>> GetProcessesAsync(this ApiClient client)
        {
            return client.GetProcessesAsync(TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /processes
        /// </summary>
        public static async Task<IEnumerable<ProcessIdentifier>> GetProcessesAsync(this ApiClient client, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessesAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{pid}
        /// </summary>
        public static Task<ProcessInfo> GetProcessAsync(this ApiClient client, int pid)
        {
            return client.GetProcessAsync(pid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /processes/{pid}
        /// </summary>
        public static async Task<ProcessInfo> GetProcessAsync(this ApiClient client, int pid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessAsync(pid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{uid}
        /// </summary>
        public static Task<ProcessInfo> GetProcessAsync(this ApiClient client, Guid uid)
        {
            return client.GetProcessAsync(uid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /processes/{uid}
        /// </summary>
        public static async Task<ProcessInfo> GetProcessAsync(this ApiClient client, Guid uid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessAsync(uid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{pid}/env
        /// </summary>
        public static Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, int pid)
        {
            return client.GetProcessEnvironmentAsync(pid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /processes/{pid}/env
        /// </summary>
        public static async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, int pid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessEnvironmentAsync(pid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{uid}/env
        /// </summary>
        public static Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, Guid uid)
        {
            return client.GetProcessEnvironmentAsync(uid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /processes/{uid}/env
        /// </summary>
        public static async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, Guid uid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessEnvironmentAsync(uid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /dump/{pid}?type={dumpType}
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, int pid, DumpType dumpType)
        {
            return client.CaptureDumpAsync(pid, dumpType, TestTimeouts.DumpTimeout);
        }

        /// <summary>
        /// Get /dump/{pid}?type={dumpType}
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, int pid, DumpType dumpType, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureDumpAsync(pid, dumpType, timeoutSource.Token);
        }

        /// <summary>
        /// Get /dump/{uid}?type={dumpType}
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, Guid uid, DumpType dumpType)
        {
            return client.CaptureDumpAsync(uid, dumpType, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /dump/{uid}?type={dumpType}
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, Guid uid, DumpType dumpType, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureDumpAsync(uid, dumpType, timeoutSource.Token);
        }

        /// <summary>
        /// GET /logs/{pid}?level={logLevel}&durationSeconds={duration}
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogLevel? logLevel)
        {
            return client.CaptureLogsAsync(pid, duration, logLevel, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /logs/{pid}?level={logLevel}&durationSeconds={duration}
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogLevel? logLevel, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureLogsAsync(pid, duration, logLevel, timeoutSource.Token);
        }

        /// <summary>
        /// POST /logs/{pid}?durationSeconds={duration}
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogsConfiguration configuration)
        {
            return client.CaptureLogsAsync(pid, duration, configuration, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// POST /logs/{pid}?durationSeconds={duration}
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogsConfiguration configuration, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureLogsAsync(pid, duration, configuration, timeoutSource.Token);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public static Task<string> GetMetricsAsync(this ApiClient client)
        {
            return client.GetMetricsAsync(TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public static async Task<string> GetMetricsAsync(this ApiClient client, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetMetricsAsync(timeoutSource.Token).ConfigureAwait(false);
        }
    }
}
