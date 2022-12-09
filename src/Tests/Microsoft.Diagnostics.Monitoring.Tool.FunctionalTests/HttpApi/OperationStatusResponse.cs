// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal sealed class OperationStatusResponse
    {
        public HttpStatusCode StatusCode { get; }

        public OperationStatus OperationStatus { get; }

        public OperationStatusResponse(HttpStatusCode statusCode, OperationStatus status)
        {
            StatusCode = statusCode;
            OperationStatus = status;
        }
    }
}
