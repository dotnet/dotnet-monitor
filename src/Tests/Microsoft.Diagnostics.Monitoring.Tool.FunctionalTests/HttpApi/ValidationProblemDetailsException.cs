// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal class ValidationProblemDetailsException : ApiStatusCodeException
    {
        public readonly ValidationProblemDetails Details;

        public ValidationProblemDetailsException(ValidationProblemDetails details, HttpStatusCode statusCode)
            : base(details.Detail, statusCode)
        {
            Details = details ?? throw new ArgumentNullException(nameof(details));
        }
    }
}
