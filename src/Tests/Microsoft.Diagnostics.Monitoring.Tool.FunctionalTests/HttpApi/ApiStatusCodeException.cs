// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal class ApiStatusCodeException : HttpRequestException
    {
        public ApiStatusCodeException(string message, HttpStatusCode statusCode)
            : base(message, null, statusCode)
        {
        }
    }
}
