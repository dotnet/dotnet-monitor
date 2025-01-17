// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal static class ApiClientExtensions
    {
        /// <summary>
        /// GET /
        /// </summary>
        public static Task<HttpResponseMessage> GetRootAsync(this ApiClient client)
        {
            return client.GetRootAsync(TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /
        /// </summary>
        public static async Task<HttpResponseMessage> GetRootAsync(this ApiClient client, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            var resp = await client.GetRootAsync(timeoutSource.Token).ConfigureAwait(false);
            return resp;
        }

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
        /// GET /processes with retry attempts
        /// </summary>
        public static async Task<IEnumerable<ProcessIdentifier>> GetProcessesWithRetryAsync(this ApiClient client, ITestOutputHelper outputHelper, int[] pidFilters, int maxAttempts = 5)
        {
            IList<ProcessIdentifier> identifiers = null;

            int attempt = 0;
            while (attempt < maxAttempts)
            {
                attempt++;

                outputHelper.WriteLine($"Attempt #{attempt} of {maxAttempts}: GET /processes");

                identifiers = (await client.GetProcessesAsync()).ToList();

                if (pidFilters.Length > 0)
                {
                    identifiers = identifiers.Where(i => pidFilters.Contains(i.Pid)).ToList();
                }

                // In .NET 5+, the process name comes from the command line from the ProcessInfo command, which can fail
                // or be abandoned if it takes too long to respond. In .NET Core 3.1, the process name comes from the
                // command line from issuing a very brief event source trace, which can also fail or be abandoned if it
                // takes too long to finish. Additionally, for 'connect' mode, these values are recomputed for every http
                // invocation, so a prior invocation could succeed whereas a subsequent one could fail. Much of this could
                // be mitigated if process information could be cached between calls. In any of these scenarios, if the
                // process name is failed to be determined, the http api will return "unknown". For testing purposes,
                // retry getting the process information until it gets the process name successfully or fail the operation
                // after a small number of attempts.
                if (null != identifiers && !identifiers.Any(identifier => string.Equals("unknown", identifier.Name, StringComparison.Ordinal)))
                {
                    return identifiers;
                }
            }

            throw new InvalidOperationException("Unable to get processes that have process names.");
        }

        /// <summary>
        /// Get /process?pid={pid}
        /// </summary>
        public static Task<ProcessInfo> GetProcessAsync(this ApiClient client, int pid)
        {
            return client.GetProcessAsync(pid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /process?pid={pid}
        /// </summary>
        public static async Task<ProcessInfo> GetProcessAsync(this ApiClient client, int pid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessAsync(pid: pid, uid: null, name: null, token: timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /process?uid={uid}
        /// </summary>
        public static Task<ProcessInfo> GetProcessAsync(this ApiClient client, Guid uid)
        {
            return client.GetProcessAsync(uid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /process?uid={uid}
        /// </summary>
        public static Task<ProcessInfo> GetProcessAsync(this ApiClient client, int? pid, Guid? uid, string name)
        {
            return client.GetProcessAsync(pid: pid, uid: uid, name: name, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /process?uid={uid}
        /// </summary>
        public static async Task<ProcessInfo> GetProcessAsync(this ApiClient client, Guid uid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessAsync(pid: null, uid: uid, name: null, token: timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// Get <![CDATA[/process?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public static async Task<ProcessInfo> GetProcessAsync(this ApiClient client, int? pid, Guid? uid, string name, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessAsync(pid: pid, uid: uid, name: name, token: timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get <![CDATA[/process?pid={pid}&uid={uid}&name={name}]]> with retry attempts.
        /// </summary>
        public static async Task<ProcessInfo> GetProcessWithRetryAsync(this ApiClient client, ITestOutputHelper outputHelper, int? pid = null, Guid? uid = null, string name = null, int maxAttempts = 5)
        {
            ProcessInfo processInfo = null;

            int attempt = 0;
            while (attempt < maxAttempts)
            {
                attempt++;

                outputHelper.WriteLine($"Attempt #{attempt} of {maxAttempts}: GET /process");
                try
                {
                    processInfo = await client.GetProcessAsync(pid: pid, uid: uid, name: name);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                // In .NET 5+, the process name comes from the command line from the ProcessInfo command, which can fail
                // or be abandoned if it takes too long to respond. In .NET Core 3.1, the process name comes from the
                // command line from issuing a very brief event source trace, which can also fail or be abandoned if it
                // takes too long to finish. Additionally, for 'connect' mode, these values are recomputed for every http
                // invocation, so a prior invocation could succeed whereas a subsequent one could fail. Much of this could
                // be mitigated if process information could be cached between calls. In any of these scenarios, if the
                // process name is failed to be determined, the http api will return "unknown". For testing purposes,
                // retry getting the process information until it gets the process name successfully or fail the operation
                // after a small number of attempts.
                if (null != processInfo && !string.Equals("unknown", processInfo.Name, StringComparison.Ordinal))
                {
                    return processInfo;
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Unable to get process information that has a process name.");
        }

        /// <summary>
        /// Get /env?pid={pid}
        /// </summary>
        public static Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, int pid)
        {
            return client.GetProcessEnvironmentAsync(pid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /env?pid={pid}
        /// </summary>
        public static async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, int pid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessEnvironmentAsync(pid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /env?uid={uid}
        /// </summary>
        public static Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, Guid uid)
        {
            return client.GetProcessEnvironmentAsync(uid, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get /env?uid={uid}
        /// </summary>
        public static async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(this ApiClient client, Guid uid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetProcessEnvironmentAsync(uid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get <![CDATA[/dump?pid={pid}&type={dumpType}]]>
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, int pid, DumpType dumpType)
        {
            return client.CaptureDumpAsync(pid, dumpType, CommonTestTimeouts.DumpTimeout);
        }

        /// <summary>
        /// Get <![CDATA[/dump?pid={pid}&type={dumpType}]]>
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, int pid, DumpType dumpType, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureDumpAsync(pid, dumpType, timeoutSource.Token);
        }

        /// <summary>
        /// Get <![CDATA[/dump?uid={uid}&type={dumpType}]]>
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, Guid uid, DumpType dumpType)
        {
            return client.CaptureDumpAsync(uid, dumpType, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Get <![CDATA[/dump?uid={uid}&type={dumpType}]]>
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureDumpAsync(this ApiClient client, Guid uid, DumpType dumpType, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureDumpAsync(uid, dumpType, timeoutSource.Token);
        }

        /// <summary>
        /// GET <![CDATA[/logs?pid={pid}&level={logLevel}&durationSeconds={duration}]]>
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat)
        {
            return client.CaptureLogsAsync(pid, duration, logLevel, TestTimeouts.HttpApi, logFormat);
        }

        /// <summary>
        /// GET <![CDATA[/logs?pid={pid}&level={logLevel}&durationSeconds={duration}]]>
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogLevel? logLevel, TimeSpan timeout, LogFormat logFormat)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureLogsAsync(pid, duration, logLevel, logFormat, timeoutSource.Token);
        }

        /// <summary>
        /// POST <![CDATA[/logs?pid={pid}&durationSeconds={duration}]]>
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat)
        {
            return client.CaptureLogsAsync(pid, duration, configuration, TestTimeouts.HttpApi, logFormat);
        }

        /// <summary>
        /// POST <![CDATA[/logs/{pid}?durationSeconds={duration}]]>
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureLogsAsync(this ApiClient client, int pid, TimeSpan duration, LogsConfiguration configuration, TimeSpan timeout, LogFormat logFormat)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureLogsAsync(pid, duration, configuration, logFormat, timeoutSource.Token);
        }

        /// <summary>
        /// GET <![CDATA[/trace?pid={pid}&profile={profile}&durationSeconds={duration}]]>
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureTraceAsync(this ApiClient client, int pid, TimeSpan duration, TraceProfile? profile)
        {
            return client.CaptureTraceAsync(pid, duration, profile, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET <![CDATA[/trace?pid={pid}&profile={profile}&durationSeconds={duration}]]>
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureTraceAsync(this ApiClient client, int pid, TimeSpan duration, TraceProfile? profile, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureTraceAsync(pid, duration, profile, timeoutSource.Token);
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

        public static async Task<ResponseStreamHolder> CaptureMetricsAsync(this ApiClient client, int processId, int durationSeconds)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(TestTimeouts.CaptureMetricsTimeout);
            return await client.CaptureMetricsAsync(processId, durationSeconds, token: timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<ResponseStreamHolder> CaptureMetricsAsync(this ApiClient client, int processId, int durationSeconds, EventMetricsConfiguration metricsConfiguration)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(TestTimeouts.CaptureMetricsTimeout);
            return await client.CaptureMetricsAsync(processId, durationSeconds, metricsConfiguration, token: timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /exceptions
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureExceptionsAsync(this ApiClient client, int processId, ExceptionFormat format)
        {
            return client.CaptureExceptionsAsync(processId, format, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /exceptions
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureExceptionsAsync(this ApiClient client, int processId, ExceptionFormat format, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureExceptionsAsync(processId, format, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// POST /exceptions
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureExceptionsAsync(this ApiClient client, ExceptionsConfiguration configuration, int processId, ExceptionFormat format)
        {
            return client.CaptureExceptionsAsync(configuration, processId, format, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// POST /exceptions
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureExceptionsAsync(this ApiClient client, ExceptionsConfiguration configuration, int processId, ExceptionFormat format, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureExceptionsAsync(configuration, processId, format, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /stacks
        /// </summary>
        public static Task<ResponseStreamHolder> CaptureStacksAsync(this ApiClient client, int pid, WebApi.StackFormat format)
        {
            return client.CaptureStacksAsync(pid, format, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /stacks
        /// </summary>
        public static async Task<ResponseStreamHolder> CaptureStacksAsync(this ApiClient client, int pid, WebApi.StackFormat format, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureStacksAsync(pid, format, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /info
        /// </summary>
        public static Task<DotnetMonitorInfo> GetInfoAsync(this ApiClient client)
        {
            return client.GetInfoAsync(TestTimeouts.HttpApi);
        }

        /// <summary>
        /// GET /info
        /// </summary>
        public static async Task<DotnetMonitorInfo> GetInfoAsync(this ApiClient client, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetInfoAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// GET <![CDATA[/collectionrules?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public static Task<Dictionary<string, CollectionRuleDescription>> GetCollectionRulesDescriptionAsync(this ApiClient client, int? pid, Guid? uid, string name)
        {
            return client.GetCollectionRulesDescriptionAsync(pid, uid, name, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// GET <![CDATA[/collectionrules?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public static async Task<Dictionary<string, CollectionRuleDescription>> GetCollectionRulesDescriptionAsync(this ApiClient client, int? pid, Guid? uid, string name, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetCollectionRulesDescriptionAsync(pid, uid, name, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// GET <![CDATA[/collectionrules/{collectionrulename}?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public static Task<CollectionRuleDetailedDescription> GetCollectionRuleDetailedDescriptionAsync(this ApiClient client, string collectionRuleName, int? pid, Guid? uid, string name)
        {
            return client.GetCollectionRuleDetailedDescriptionAsync(collectionRuleName, pid, uid, name, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// GET <![CDATA[/collectionrules/{collectionrulename}?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public static async Task<CollectionRuleDetailedDescription> GetCollectionRuleDetailedDescriptionAsync(this ApiClient client, string collectionRuleName, int? pid, Guid? uid, string name, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.GetCollectionRuleDetailedDescriptionAsync(collectionRuleName, pid, uid, name, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<OperationResponse> EgressTraceAsync(this ApiClient client, int processId, int durationSeconds, string egressProvider, string tags = null)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.EgressTraceAsync(processId, durationSeconds, egressProvider, tags, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<ResponseStreamHolder> HttpEgressTraceAsync(this ApiClient client, int processId, int durationSeconds)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.HttpEgressTraceAsync(processId, durationSeconds, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<OperationStatusResponse> GetOperationStatus(this ApiClient client, Uri operation)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.GetOperationStatus(operation, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<List<OperationSummary>> GetOperations(this ApiClient client, string tags = null)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.GetOperations(tags, timeoutSource.Token).ConfigureAwait(false);
        }

        public static Task<HttpStatusCode> StopEgressOperation(this ApiClient client, Guid operationId)
        {
            return StopEgressOperation(client, new Uri(client.BaseAddress, FormattableString.Invariant($"operations/{operationId}")));
        }

        public static async Task<HttpStatusCode> StopEgressOperation(this ApiClient client, Uri operation)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.StopEgressOperation(operation, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<HttpStatusCode> CancelEgressOperation(this ApiClient client, Uri operation)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.CancelEgressOperation(operation, timeoutSource.Token).ConfigureAwait(false);
        }

        public static async Task<HttpResponseMessage> ApiCall(this ApiClient client, string routeAndQuery)
        {
            using CancellationTokenSource timeoutSource = new(TestTimeouts.HttpApi);
            return await client.ApiCall(routeAndQuery, timeoutSource.Token).ConfigureAwait(false);
        }

        public static Task<OperationStatusResponse> PollOperationToCompletion(this ApiClient apiClient, Uri operationUrl)
        {
            return apiClient.PollOperationToCompletion(operationUrl, TestTimeouts.OperationTimeout);
        }

        public static async Task<OperationStatusResponse> PollOperationToCompletion(this ApiClient apiClient, Uri operationUrl, TimeSpan timeout)
        {
            OperationStatusResponse operationResult = await apiClient.GetOperationStatus(operationUrl).ConfigureAwait(false);
            Assert.True(operationResult.StatusCode == HttpStatusCode.OK || operationResult.StatusCode == HttpStatusCode.Created);

            using CancellationTokenSource cancellationTokenSource = new(timeout);
            while (operationResult.OperationStatus.Status == OperationState.Starting ||
                operationResult.OperationStatus.Status == OperationState.Running ||
                operationResult.OperationStatus.Status == OperationState.Stopping)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token).ConfigureAwait(false);
                operationResult = await apiClient.GetOperationStatus(operationUrl).ConfigureAwait(false);
            }

            return operationResult;
        }

        /// <summary>
        /// POST /parameters
        /// </summary>
        public static Task<OperationResponse> CaptureParametersAsync(this ApiClient client, int pid, TimeSpan duration, CaptureParametersConfiguration config)
        {
            return client.CaptureParametersAsync(pid, duration, config, CapturedParameterFormat.JsonSequence, string.Empty, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// POST /parameters
        /// </summary>
        public static Task<OperationResponse> CaptureParametersAsync(this ApiClient client, int pid, TimeSpan duration, CaptureParametersConfiguration config, CapturedParameterFormat format)
        {
            return client.CaptureParametersAsync(pid, duration, config, format, string.Empty, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// POST /parameters
        /// </summary>
        public static Task<OperationResponse> CaptureParametersAsync(this ApiClient client, int pid, TimeSpan duration, CaptureParametersConfiguration config, CapturedParameterFormat format, string egressProvider)
        {
            return client.CaptureParametersAsync(pid, duration, config, format, egressProvider, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// POST /parameters
        /// </summary>
        public static async Task<OperationResponse> CaptureParametersAsync(this ApiClient client, int pid, TimeSpan duration, CaptureParametersConfiguration config, CapturedParameterFormat format, string egressProvider, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await client.CaptureParametersAsync(pid, duration, egressProvider, config, format, timeoutSource.Token).ConfigureAwait(false);
        }

        public static Task<OperationStatusResponse> WaitForOperationToStart(this ApiClient apiClient, Uri operationUrl)
        {
            return apiClient.WaitForOperationToStart(operationUrl, TestTimeouts.OperationTimeout);
        }

        public static async Task<OperationStatusResponse> WaitForOperationToStart(this ApiClient apiClient, Uri operationUrl, TimeSpan timeout)
        {
            OperationStatusResponse operationResult = await apiClient.GetOperationStatus(operationUrl).ConfigureAwait(false);
            Assert.True(operationResult.StatusCode == HttpStatusCode.OK || operationResult.StatusCode == HttpStatusCode.Created);

            using CancellationTokenSource cancellationTokenSource = new(timeout);
            while (operationResult.OperationStatus.Status == OperationState.Starting)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token).ConfigureAwait(false);
                operationResult = await apiClient.GetOperationStatus(operationUrl).ConfigureAwait(false);
            }

            return operationResult;
        }

    }
}
