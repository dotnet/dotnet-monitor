// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        public static void MapMetricsActionMethods(IEndpointRouteBuilder builder)
        {
            // CaptureMetrics
            builder.MapGet("livemetrics",
                [EndpointSummary("Capture metrics for a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [Description("Process ID used to identify the target process.")]
                int? pid,
                [Description("The Runtime instance cookie used to identify the target process.")]
                Guid? uid,
                [Description("Process name used to identify the target process.")]
                string? name,
                [Range(-1, int.MaxValue)]
                [Description("The duration of the metrics session (in seconds).")]
                int durationSeconds = 30,
                [Description("The egress provider to which the metrics are saved.")]
                string? egressProvider = null,
                [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")]
                string? tags = null) =>
                    new DiagController(context.RequestServices, logger).CaptureMetrics(pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureMetrics))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureMetricsCustom
            builder.MapPost("livemetrics",
                [EndpointSummary("Capture metrics for a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromBody][Required][Description("The metrics configuration describing which metrics to capture.")]
                Models.EventMetricsConfiguration configuration,
                [Description("Process ID used to identify the target process.")]
                int? pid,
                [Description("The Runtime instance cookie used to identify the target process.")]
                Guid? uid,
                [Description("Process name used to identify the target process.")]
                string? name,
                [Range(-1, int.MaxValue)]
                [Description("The duration of the metrics session (in seconds).")]
                int durationSeconds = 30,
                [Description("The egress provider to which the metrics are saved.")]
                string? egressProvider = null,
                [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")]
                string? tags = null) =>
                    new DiagController(context.RequestServices, logger).CaptureMetricsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureMetricsCustom))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .Accepts<Models.EventMetricsConfiguration>(ContentTypes.ApplicationJson, ContentTypes.TextJson, ContentTypes.ApplicationAnyJson)
                .RequireEgressValidation();
        }

        public Task<IResult> CaptureMetrics(
            int? pid,
            Guid? uid,
            string? name,
            int durationSeconds,
            string? egressProvider,
            string? tags)
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

        public Task<IResult> CaptureMetricsCustom(
            Models.EventMetricsConfiguration configuration,
            int? pid,
            Guid? uid,
            string? name,
            int durationSeconds,
            string? egressProvider,
            string? tags)
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
