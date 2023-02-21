// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        /// <summary>
        /// Capture metrics for a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpGet("livemetrics", Name = nameof(CaptureMetrics))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = Utilities.ArtifactType_Metrics)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetrics(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = MetricsUtilities.GetMetricFilename(processInfo.EndpointInfo);

                MetricsPipelineSettings settings = MetricsSettingsFactory.CreateSettings(
                    _counterOptions.CurrentValue,
                    includeDefaults: true,
                    durationSeconds: durationSeconds);

                return await Result(Utilities.ArtifactType_Metrics,
                    egressProvider,
                    (outputStream, token) => MetricsUtilities.CaptureLiveMetricsAsync(null, processInfo.EndpointInfo, settings, outputStream, token),
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo);
            }, processKey, Utilities.ArtifactType_Metrics);
        }

        /// <summary>
        /// Capture metrics for a process.
        /// </summary>
        /// <param name="configuration">The metrics configuration describing which metrics to capture.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        [HttpPost("livemetrics", Name = nameof(CaptureMetricsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = Utilities.ArtifactType_Metrics)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetricsCustom(
            [FromBody][Required]
            Models.EventMetricsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(async (processInfo) =>
            {
                string fileName = MetricsUtilities.GetMetricFilename(processInfo.EndpointInfo);

                MetricsPipelineSettings settings = MetricsSettingsFactory.CreateSettings(
                    _counterOptions.CurrentValue,
                    durationSeconds,
                    configuration);

                return await Result(Utilities.ArtifactType_Metrics,
                    egressProvider,
                    (outputStream, token) => MetricsUtilities.CaptureLiveMetricsAsync(null, processInfo.EndpointInfo, settings, outputStream, token),
                    fileName,
                    ContentTypes.ApplicationJsonSequence,
                    processInfo);
            }, processKey, Utilities.ArtifactType_Metrics);
        }
    }
}
