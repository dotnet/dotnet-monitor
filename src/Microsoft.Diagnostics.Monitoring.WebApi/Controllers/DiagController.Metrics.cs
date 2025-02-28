﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    partial class DiagController
    {
        public DiagController MapMetricsActionMethods(IEndpointRouteBuilder builder)
        {
            builder.MapGet("livemetrics", (
                [FromQuery]
                int? pid,
                [FromQuery]
                Guid? uid,
                [FromQuery]
                string? name,
                [FromQuery][Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                [FromQuery]
                string? egressProvider = null,
                [FromQuery]
                string? tags = null) =>
                    CaptureMetrics(pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureMetrics))
                .RequireHostRestriction()
                .RequireAuthorization(AuthConstants.PolicyName)
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            builder.MapGet("livemetrics", (
                [FromBody][Required]
                Models.EventMetricsConfiguration configuration,
                [FromQuery]
                int? pid,
                [FromQuery]
                Guid? uid,
                [FromQuery]
                string? name,
                [FromQuery][Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                [FromQuery]
                string? egressProvider = null,
                [FromQuery]
                string? tags = null) =>
                    CaptureMetricsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureMetricsCustom))
                .RequireHostRestriction()
                .RequireAuthorization(AuthConstants.PolicyName)
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            return this;
        }

        /// <summary>
        /// Capture metrics for a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the metrics session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the metrics are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
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
