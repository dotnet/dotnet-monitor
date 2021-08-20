// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        /// <summary>
        /// Capture live metrics for a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="metricsIntervalSeconds">The reporting interval (in seconds) for event counters.</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpGet("livemetrics", Name = nameof(LiveMetrics))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_LiveMetrics)]
        [EgressValidation]
        public Task<ActionResult> LiveMetrics(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery][Range(1, int.MaxValue)]
            int metricsIntervalSeconds = 5,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = GetMetricFilename(processInfo);

                Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
                {
                    var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);
                    EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                        includeDefaults: true,
                        durationSeconds: durationSeconds,
                        refreshInterval: metricsIntervalSeconds);

                    await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                        settings,
                        loggers:
                        new[] { new JsonCounterLogger(outputStream) });

                    await eventCounterPipeline.RunAsync(token);
                };

                return await Result(ArtifactType_LiveMetrics,
                    egressProvider,
                    action,
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo.EndpointInfo);
            }, processKey, ArtifactType_LiveMetrics);
        }

        /// <summary>
        /// Capture live metrics for a process.
        /// </summary>
        /// <param name="configuration">The metrics configuration describing which metrics to capture.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="metricsIntervalSeconds">The reporting interval (in seconds) for event counters.</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpPost("livemetrics", Name = nameof(LiveMetricsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_LiveMetrics)]
        [EgressValidation]
        public Task<ActionResult> LiveMetricsCustom(
            [FromBody][Required]
            Models.EventMetrics configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery][Range(1, int.MaxValue)]
            int metricsIntervalSeconds = 5,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = GetMetricFilename(processInfo);

                Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
                {
                    var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);
                    EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                        durationSeconds,
                        metricsIntervalSeconds,
                        configuration);

                    await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                        settings,
                        loggers:
                        new[] { new JsonCounterLogger(outputStream) });

                    await eventCounterPipeline.RunAsync(token);
                };

                return await Result(ArtifactType_LiveMetrics,
                    egressProvider,
                    action,
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo.EndpointInfo);
            }, processKey, ArtifactType_LiveMetrics);
        }

        private static string GetMetricFilename(IProcessInfo processInfo) =>
            FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.metrics.json");
    }
}
