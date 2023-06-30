// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("")]
    [ApiController]
    [HostRestriction]
    [Authorize(Policy = AuthConstants.PolicyName)]
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public sealed class ExceptionsController : ControllerBase
    {
        private readonly IExceptionsStore _exceptionsStore;
        private readonly IOptions<ExceptionsOptions> _exceptionsOptions;
        private readonly IExceptionsOperationFactory _operationFactory;

        public ExceptionsController(IServiceProvider serviceProvider)
        {
            // The exceptions store for the default process
            _exceptionsStore = serviceProvider.GetRequiredService<IExceptionsStore>();
            _exceptionsOptions = serviceProvider.GetRequiredService<IOptions<ExceptionsOptions>>();
            _operationFactory = serviceProvider.GetRequiredService<IExceptionsOperationFactory>();
        }

        /// <summary>
        /// Gets the exceptions from the default process.
        /// </summary>
        [HttpGet("exceptions", Name = nameof(GetExceptions))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [EgressValidation]
        public ActionResult GetExceptions()
        {
            if (!_exceptionsOptions.Value.GetEnabled())
            {
                return NotFound();
            }

            ExceptionsFormat? format = ComputeFormat(Request.GetTypedHeaders().Accept);
            if (!format.HasValue)
            {
                return this.NotAcceptable();
            }

            IArtifactOperation operation = _operationFactory.Create(_exceptionsStore, format.Value);

            return new OutputStreamResult(operation);
        }

        private static ExceptionsFormat? ComputeFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            if (acceptedHeaders == null || acceptedHeaders.Count == 0)
            {
                return null;
            }

            if (acceptedHeaders.Contains(ContentTypeUtilities.TextPlainHeader))
            {
                return ExceptionsFormat.PlainText;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.NdJsonHeader))
            {
                return ExceptionsFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.JsonSequenceHeader))
            {
                return ExceptionsFormat.JsonSequence;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.TextPlainHeader.IsSubsetOf))
            {
                return ExceptionsFormat.PlainText;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.NdJsonHeader.IsSubsetOf))
            {
                return ExceptionsFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.JsonSequenceHeader.IsSubsetOf))
            {
                return ExceptionsFormat.JsonSequence;
            }
            return null;
        }
    }
}
