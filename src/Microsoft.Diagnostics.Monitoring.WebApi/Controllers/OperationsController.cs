// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    static partial class RouteHandlerBuilderExtensions
    {
        public static RouteHandlerBuilder RequireOperationsControllerCommon(this RouteHandlerBuilder builder)
        {
            return builder
                .RequireHostRestriction()
                .RequireAuthorization(AuthConstants.PolicyName)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithTags("Operations");
        }
    }

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

        // Todo: use MapGroup!
        // Factor out.
        public static void MapActionMethods(IEndpointRouteBuilder builder)
        {
            // GetOperations
            builder.MapGet($"{ControllerName}", [EndpointSummary("Gets the operations list for the specified process (or all processes if left unspecified).")] (
                HttpContext context,
                ILogger<OperationsController> logger,
                [Description("Process ID used to identify the target process.")] int? pid,
                [Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [Description("Process name used to identify the target process.")] string? name,
                [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags) =>
                    new OperationsController(logger, context.RequestServices).GetOperations(pid, uid, name, tags))
            .WithName(nameof(GetOperations))
            .RequireOperationsControllerCommon()
            .Produces<IEnumerable<OperationSummary>>(StatusCodes.Status200OK);

            // GetOperationStatus
            builder.MapGet($"{ControllerName}/{{operationId}}", (
                HttpContext context,
                ILogger<OperationsController> logger,
                Guid operationId) =>
                    new OperationsController(logger, context.RequestServices).GetOperationStatus(operationId))
            .WithName(nameof(GetOperationStatus))
            .RequireOperationsControllerCommon()
            .Produces<OperationStatus>(StatusCodes.Status200OK)
            .Produces<OperationStatus>(StatusCodes.Status201Created);

            // CancelOperation
            builder.MapDelete($"{ControllerName}/{{operationId}}", (
                HttpContext context,
                ILogger<OperationsController> logger,
                Guid operationId,
                bool stop = false) =>
                    new OperationsController(logger, context.RequestServices).CancelOperation(operationId, stop))
            .WithName(nameof(CancelOperation))
            .RequireOperationsControllerCommon()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status202Accepted);
        }

        /// <summary>
        /// Gets the operations list for the specified process (or all processes if left unspecified).
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        public IResult GetOperations(
            int? pid,
            Guid? uid,
            string? name,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return this.InvokeService(() =>
            {
                return Results.Ok(_operationsStore.GetOperations(processKey, tags));
            }, _logger);
        }

        public IResult GetOperationStatus(Guid operationId)
        {
            return this.InvokeService(() =>
            {
                Models.OperationStatus status = _operationsStore.GetOperationStatus(operationId);
                return status.Status == Models.OperationState.Succeeded
#pragma warning disable CS8625 // Implementation accexts null, but nullable annotation was added in .NET 9
                    ? TypedResults.Created((string?)null, status)
#pragma warning restore CS8625
                    : Results.Ok(status);
            }, _logger);
        }

        public IResult CancelOperation(
            Guid operationId,
            bool stop)
        {
            return this.InvokeService<Results<Accepted, Ok>>(() =>
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
                    return TypedResults.Accepted((string?)null);
                }
                else
                {
                    _operationsStore.CancelOperation(operationId);
                    return TypedResults.Ok();
                }
            }, _logger);
        }
    }
}
