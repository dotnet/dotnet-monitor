// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
{
    internal sealed class OperationResponse
    {
        public Uri OperationUri { get; }

        public HttpStatusCode StatusCode { get; }

        public string ResponseBody { get; }

        public OperationResponse(HttpStatusCode statusCode, Uri operationUri = null, string responseBody = null)
        {
            OperationUri = operationUri;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
