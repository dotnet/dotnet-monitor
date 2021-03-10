// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
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
