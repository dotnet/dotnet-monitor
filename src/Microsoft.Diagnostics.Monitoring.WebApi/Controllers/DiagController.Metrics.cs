// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        [EndpointSummary("Capture metrics for a process.")]
        [HttpGet("livemetrics", Name = nameof(CaptureMetrics))]
        [ProducesWithProblemDetails]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetrics(
            [FromQuery]
            [Description("Process ID used to identify the target process.")]
            int? pid = null,
            [FromQuery]
            [Description("The Runtime instance cookie used to identify the target process.")]
            Guid? uid = null,
            [FromQuery]
            [Description("Process name used to identify the target process.")]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            [Description("The duration of the metrics session (in seconds).")]
            int durationSeconds = 30,
            [FromQuery]
            [Description("The egress provider to which the metrics are saved.")]
            string? egressProvider = null,
            [FromQuery]
            [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")]
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

        [EndpointSummary("Capture metrics for a process.")]
        [HttpPost("livemetrics", Name = nameof(CaptureMetricsCustom))]
        [ProducesWithProblemDetails]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureMetricsCustom(
            [FromBody][Required]
            [Description("The metrics configuration describing which metrics to capture.")]
            Models.EventMetricsConfiguration configuration,
            [FromQuery]
            [Description("Process ID used to identify the target process.")]
            int? pid = null,
            [FromQuery]
            [Description("The Runtime instance cookie used to identify the target process.")]
            Guid? uid = null,
            [FromQuery]
            [Description("Process name used to identify the target process.")]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            [Description("The duration of the metrics session (in seconds).")]
            int durationSeconds = 30,
            [FromQuery]
            [Description("The egress provider to which the metrics are saved.")]
            string? egressProvider = null,
            [FromQuery]
            [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")]
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
