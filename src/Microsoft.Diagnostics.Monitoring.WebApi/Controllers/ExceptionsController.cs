﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    static partial class RouteHandlerBuilderExtensions
    {
        public static RouteHandlerBuilder RequireExceptionsControllerCommon(this RouteHandlerBuilder builder)
        {
            return builder
                .RequireHostRestriction()
                .RequireAuthorization(AuthConstants.PolicyName)
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                .WithTags("Exceptions");
        }
    }

    public sealed class ExceptionsController :
        DiagnosticsControllerBase
    {
        private readonly IOptions<ExceptionsOptions> _options;

        public ExceptionsController(
            IServiceProvider serviceProvider,
            ILogger<ExceptionsController> logger)
            : base(serviceProvider, logger)
        {
            _options = serviceProvider.GetRequiredService<IOptions<ExceptionsOptions>>();
        }

        public static void MapActionMethods(IEndpointRouteBuilder builder)
        {
            // GetExceptions
            builder.MapGet("exceptions",
                [EndpointSummary("Gets the exceptions from the target process.")] (
                HttpContext context,
                ILogger<ExceptionsController> logger,
                [Description("Process ID used to identify the target process.")] int? pid,
                [Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [Description("Process name used to identify the target process.")] string? name,
                [Description("The egress provider to which the exceptions are saved.")] string? egressProvider = null,
                [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new ExceptionsController(context.RequestServices, logger).GetExceptions(pid, uid, name, egressProvider, tags))
                .WithName(nameof(GetExceptions))
                .RequireExceptionsControllerCommon()
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();
            
            // CaptureExceptionsCustom
            builder.MapPost("exceptions",
                [EndpointSummary("Gets the exceptions from the target process.")] (
                HttpContext context,
                ILogger<ExceptionsController> logger,
                [FromBody, Description("The exceptions configuration describing which exceptions to include in the response.")]
                ExceptionsConfiguration configuration,
                [Description("Process ID used to identify the target process.")] int? pid,
                [Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [Description("Process name used to identify the target process.")] string? name,
                [Description("The egress provider to which the exceptions are saved.")] string? egressProvider = null,
                [Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new ExceptionsController(context.RequestServices, logger).CaptureExceptionsCustom(configuration, pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureExceptionsCustom))
                .RequireExceptionsControllerCommon()
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .Accepts<ExceptionsConfiguration>(ContentTypes.ApplicationJson, ContentTypes.TextJson, ContentTypes.ApplicationAnyJson)
                .RequireEgressValidation();
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        public Task<IResult> GetExceptions(
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags)
        {
            if (!_options.Value.GetEnabled())
            {
                return Task.FromResult<IResult>(this.FeatureNotEnabled(Strings.FeatureName_Exceptions));
            }

            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                ExceptionFormat format = ComputeFormat(Request.GetTypedHeaders().Accept) ?? ExceptionFormat.PlainText;

                IArtifactOperation operation = processInfo.EndpointInfo.ServiceProvider
                    .GetRequiredService<IExceptionsOperationFactory>()
                    .Create(format, new ExceptionsConfigurationSettings());

                return Result(
                    Utilities.ArtifactType_Exceptions,
                    egressProvider,
                    operation,
                    processInfo,
                    tags,
                    format != ExceptionFormat.PlainText);
            }, processKey, Utilities.ArtifactType_Exceptions);
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <param name="configuration">The exceptions configuration describing which exceptions to include in the response.</param>
        public Task<IResult> CaptureExceptionsCustom(
            ExceptionsConfiguration configuration,
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags)
        {
            if (!_options.Value.GetEnabled())
            {
                return Task.FromResult<IResult>(TypedResults.NotFound());
            }
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                ExceptionFormat format = ComputeFormat(Request.GetTypedHeaders().Accept) ?? ExceptionFormat.PlainText;

                IArtifactOperation operation = processInfo.EndpointInfo.ServiceProvider
                    .GetRequiredService<IExceptionsOperationFactory>()
                    .Create(format, ExceptionsSettingsFactory.ConvertExceptionsConfiguration(configuration));

                return Result(
                    Utilities.ArtifactType_Exceptions,
                    egressProvider,
                    operation,
                    processInfo,
                    tags,
                    format != ExceptionFormat.PlainText);
            }, processKey, Utilities.ArtifactType_Exceptions);
        }

        private static ExceptionFormat? ComputeFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            if (acceptedHeaders == null || acceptedHeaders.Count == 0)
            {
                return null;
            }

            if (acceptedHeaders.Contains(ContentTypeUtilities.TextPlainHeader))
            {
                return ExceptionFormat.PlainText;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.NdJsonHeader))
            {
                return ExceptionFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.JsonSequenceHeader))
            {
                return ExceptionFormat.JsonSequence;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.TextPlainHeader.IsSubsetOf))
            {
                return ExceptionFormat.PlainText;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.NdJsonHeader.IsSubsetOf))
            {
                return ExceptionFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.JsonSequenceHeader.IsSubsetOf))
            {
                return ExceptionFormat.JsonSequence;
            }
            return null;
        }
    }
}
