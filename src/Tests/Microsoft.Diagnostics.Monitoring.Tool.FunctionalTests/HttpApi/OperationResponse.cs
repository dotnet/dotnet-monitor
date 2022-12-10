// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal sealed class OperationResponse
    {
        public Uri OperationUri { get; }

        public HttpStatusCode StatusCode { get; }

        public OperationResponse(HttpStatusCode statusCode, Uri operationUri = null)
        {
            OperationUri = operationUri;
            StatusCode = statusCode;
        }
    }
}
