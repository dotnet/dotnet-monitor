// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    public class MetricsController : MinimalControllerBase
    {
        private const string ArtifactType_Metrics = "metrics";

        private readonly ILogger<MetricsController> _logger;
        private readonly MetricsStoreService _metricsStore;
        private readonly MetricsOptions _metricsOptions;

        public MetricsController(ILogger<MetricsController> logger, HttpContext httpContext, IOptions<MetricsOptions> metricsOptions) :
            base(httpContext)
        {
            _logger = logger;
            _metricsStore = httpContext.RequestServices.GetRequiredService<MetricsStoreService>();
            _metricsOptions = metricsOptions.Value;
        }

        public static void MapActionMethods(IEndpointRouteBuilder builder)
        {
            // GetMetrics
            builder.MapGet("metrics",
                [EndpointSummary("Get a list of the current backlog of metrics for a process in the Prometheus exposition format.")] (
                ILogger<MetricsController> logger,
                HttpContext context,
                IOptions<MetricsOptions> metricsOptions) =>
                    new MetricsController(logger, context, metricsOptions).GetMetrics())
                .WithName(nameof(GetMetrics))
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.TextPlain_v0_0_4)
                .WithTags("Metrics");
        }

        /// <summary>
        /// Get a list of the current backlog of metrics for a process in the Prometheus exposition format.
        /// </summary>
        public IResult GetMetrics()
        {
            return this.InvokeService(() =>
            {
                if (!_metricsOptions.GetEnabled())
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_MetricsDisabled);
                }

                KeyValueLogScope scope = new KeyValueLogScope();
                scope.AddArtifactType(ArtifactType_Metrics);

                return new OutputStreamResult(async (outputStream, token) =>
                    {
                        await _metricsStore.MetricsStore.SnapshotMetrics(outputStream, token);
                    },
                    ContentTypes.TextPlain_v0_0_4,
                    null,
                    scope);
            }, _logger);
        }
    }
}
