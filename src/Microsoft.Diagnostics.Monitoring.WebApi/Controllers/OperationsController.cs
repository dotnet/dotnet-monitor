﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route(ControllerName)]
    [ApiController]
    [HostRestriction]
    [Authorize(Policy = AuthConstants.PolicyName)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    public class OperationsController : ControllerBase
    {
        private readonly ILogger<OperationsController> _logger;
        private readonly IEgressOperationStore _operationsStore;

        public const string ControllerName = "operations";

        public OperationsController(ILogger<OperationsController> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _operationsStore = serviceProvider.GetRequiredService<IEgressOperationStore>();
        }

        [HttpGet(Name = nameof(GetOperations))]
        [ProducesWithProblemDetails]
        [ProducesResponseType(typeof(IEnumerable<Models.OperationSummary>), StatusCodes.Status200OK, ContentTypes.ApplicationJson)]
        [EndpointSummary("Gets the operations list for the specified process (or all processes if left unspecified).")]
        public ActionResult<IEnumerable<Models.OperationSummary>> GetOperations(
            [FromQuery]
            [Description("Process ID used to identify the target process.")]
            int? pid = null,
            [FromQuery]
            [Description("The Runtime instance cookie used to identify the target process.")]
            Guid? uid = null,
            [FromQuery]
            [Description("Process name used to identify the target process.")]
            string? name = null,
            [FromQuery]
            [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")]
            string? tags = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return this.InvokeService(() =>
            {
                return new ActionResult<IEnumerable<Models.OperationSummary>>(_operationsStore.GetOperations(processKey, tags));
            }, _logger);
        }

        [HttpGet("{operationId}", Name = nameof(GetOperationStatus))]
        [ProducesWithProblemDetails]
        [ProducesResponseType(typeof(Models.OperationStatus), StatusCodes.Status201Created, ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.OperationStatus), StatusCodes.Status200OK, ContentTypes.ApplicationJson)]
        public IActionResult GetOperationStatus(Guid operationId)
        {
            return this.InvokeService(() =>
            {
                Models.OperationStatus status = _operationsStore.GetOperationStatus(operationId);
                int statusCode = (int)(status.Status == Models.OperationState.Succeeded ? StatusCodes.Status201Created : StatusCodes.Status200OK);
                return this.StatusCode(statusCode, status);
            }, _logger);
        }

        [HttpDelete("{operationId}", Name = nameof(CancelOperation))]
        [ProducesWithProblemDetails]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        public IActionResult CancelOperation(
            Guid operationId,
            [FromQuery]
            bool stop = false)
        {
            return this.InvokeService(() =>
            {
                //Note that if the operation is not found, it will throw an InvalidOperationException and
                //return an error code.
                if (stop)
                {
                    // If stopping an operation fails, it's undefined behavior.
                    // Leave the operation in the "Stopping" state and it'll either complete on its own
                    // or the user will cancel it.
                    _operationsStore.StopOperation(operationId, (ex) => _logger.StopOperationFailed(operationId, ex));

                    // Stop operations are not instant, they are instead queued and can take an indeterminate amount of time.
                    return Accepted();
                }
                else
                {
                    _operationsStore.CancelOperation(operationId);
                    return Ok();
                }
            }, _logger);
        }
    }
}
