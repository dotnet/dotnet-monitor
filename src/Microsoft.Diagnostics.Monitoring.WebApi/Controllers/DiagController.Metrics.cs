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
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        [HttpGet("livemetrics", Name = nameof(CaptureMetrics))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetrics(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            MetricsPipelineSettings settings = MetricsSettingsFactory.CreateSettings(
                _counterOptions.CurrentValue,
                durationSeconds,
                _metricsOptions.CurrentValue);

            return InvokeForProcess(
                processInfo => Result(
                    Utilities.ArtifactType_Metrics,
                    egressProvider,
                    _metricsOperationFactory.Create(processInfo.EndpointInfo, settings),
                    processInfo,
                    tags),
                processKey,
                Utilities.ArtifactType_Metrics);
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
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        [HttpPost("livemetrics", Name = nameof(CaptureMetricsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetricsCustom(
            [FromBody][Required]
            Models.EventMetricsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            MetricsPipelineSettings settings = MetricsSettingsFactory.CreateSettings(
                _counterOptions.CurrentValue,
                durationSeconds,
                configuration);

            return InvokeForProcess(
                processInfo => Result(
                    Utilities.ArtifactType_Metrics,
                    egressProvider,
                    _metricsOperationFactory.Create(processInfo.EndpointInfo, settings),
                    processInfo,
                    tags),
                processKey,
                Utilities.ArtifactType_Metrics);
        }
    }
}
