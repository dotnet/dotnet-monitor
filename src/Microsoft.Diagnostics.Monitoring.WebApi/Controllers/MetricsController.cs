// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("")]
    [ApiController]
#if NETCOREAPP3_1_OR_GREATER
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
#endif
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public class MetricsController : ControllerBase
    {
        private const string ArtifactType_Metrics = "metrics";

        private readonly ILogger<MetricsController> _logger;
        private readonly MetricsStoreService _metricsStore;
        private readonly MetricsOptions _metricsOptions;

        public MetricsController(ILogger<MetricsController> logger,
            IServiceProvider serviceProvider,
            IOptions<MetricsOptions> metricsOptions)
        {
            _logger = logger;
            _metricsStore = serviceProvider.GetRequiredService<MetricsStoreService>();
            _metricsOptions = metricsOptions.Value;
        }

        /// <summary>
        /// Gets a list of the current backlog of metrics for default process (or multiple processes) in the Prometheus exposition format.
        /// Behavior of this endpoint depends on <c>AllowMultipleProcessesMetrics</c>  configuration flag:
        ///  - if <c>false</c>: data will be returned only if exactly one process matches default process filters.
        ///  - if <c>true</c>: data will be returned for all processes which match filters, event if there is more than one.
        ///
        /// Returned metrics contain <c>process_id</c> and <c>process_name</c> labels, which can be used
        /// to determine metric origin process.
        /// </summary>
        [HttpGet("metrics", Name = nameof(GetMetrics))]
        [ProducesWithProblemDetails(ContentTypes.TextPlain_v0_0_4)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult GetMetrics()
        {
            if (!_metricsOptions.AllowMultipleProcessesMetrics)
            {
                return GetSingleProcessMetrics();
            }

            return GetMultipleProcessesMetrics();
        }

        private ActionResult GetSingleProcessMetrics()
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

        private ActionResult GetMultipleProcessesMetrics()
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
                        foreach (MetricsStore metricsStore in _metricsStore.GetAllMetrics())
                        {
                            await metricsStore.SnapshotMetrics(outputStream, token);
                        }
                    },
                    ContentTypes.TextPlain_v0_0_4,
                    null,
                    scope);
            }, _logger);
        }
    }
}
