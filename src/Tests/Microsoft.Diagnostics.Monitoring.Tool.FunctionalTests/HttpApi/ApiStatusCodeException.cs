// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal class ApiStatusCodeException : HttpRequestException
    {
        public ApiStatusCodeException(string message, HttpStatusCode statusCode)
#if NET5_0_OR_GREATER
            : base(message, null, statusCode)
        {
        }
#else
            : base(message)
        {
            StatusCode = statusCode;
        }
#endif

#if !NET5_0_OR_GREATER
        public HttpStatusCode StatusCode { get; set; }
#endif
    }
}
