// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class ExceptionExtensions
    {
        public static ProblemDetails ToProblemDetails(this Exception ex, int statusCode)
        {
            return new ProblemDetails
            {
                Detail = ex.Message,
                Status = statusCode
            };
        }
    }
}
