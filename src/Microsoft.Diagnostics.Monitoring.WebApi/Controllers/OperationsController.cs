// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
        private readonly EgressOperationStore _operationsStore;

        public const string ControllerName = "operations";

        public OperationsController(ILogger<OperationsController> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _operationsStore = serviceProvider.GetRequiredService<EgressOperationStore>();
        }

        /// <summary>
        /// Gets the operations list for the specified process (or all processes if left unspecified).
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        [HttpGet]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<Models.OperationSummary>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Models.OperationSummary>> GetOperations(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return this.InvokeService(() =>
            {
                return new ActionResult<IEnumerable<Models.OperationSummary>>(_operationsStore.GetOperations(processKey));
            }, _logger);
        }

        [HttpGet("{operationId}")]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.OperationStatus), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Models.OperationStatus), StatusCodes.Status200OK)]
        public IActionResult GetOperationStatus(Guid operationId)
        {
            return this.InvokeService(() =>
            {
                Models.OperationStatus status = _operationsStore.GetOperationStatus(operationId);
                int statusCode = (int)(status.Status == Models.OperationState.Succeeded ? StatusCodes.Status201Created : StatusCodes.Status200OK);
                return this.StatusCode(statusCode, status);
            }, _logger);
        }

        [HttpDelete("{operationId}")]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public IActionResult CancelOperation(Guid operationId)
        {
            return this.InvokeService(() =>
            {
                //Note that if the operation is not found, it will throw an InvalidOperationException and
                //return an error code.
                _operationsStore.CancelOperation(operationId);
                return Ok();
            }, _logger);
        }
    }
}
