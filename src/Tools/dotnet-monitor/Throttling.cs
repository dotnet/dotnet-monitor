// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Limits the amount of requests that can be sent to the server.
    /// - The current rate limits are based on concurrent requests to the server from any source on a per endpoint basis.
    /// - Note we do not use Microsoft.AspNetCore.ConcurrencyLimiter because it works over the whole application instead of per endpoint.
    /// - In the future, we may want to switch to https://github.com/dotnet/aspnetcore/issues/29933
    /// TODO For asp.net 2.1, this would be implemented as an ActionFilter. For 3.1+, we use an endpoints + middleware
    /// </summary>
    internal sealed class Throttling
    {

        private readonly RequestDelegate _next;

        private readonly RequestLimitTracker _limitTracker;
        private const string EgressQuery = "egressprovider";

        public Throttling(RequestDelegate next, RequestLimitTracker requestLimitTracker)
        {
            _next = next;
            _limitTracker = requestLimitTracker;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            RequestLimitAttribute requestLimit = endpoint?.Metadata.GetMetadata<RequestLimitAttribute>();
            IDisposable incrementor = null;

            try
            {
                //Operations and middleware both share the same increment limits, but
                //we don't want the middleware to increment the limit if the operation is doing it as well.
                if ((requestLimit != null) && !context.Request.Query.ContainsKey(EgressQuery))
                {
                    incrementor = _limitTracker.Increment(requestLimit.LimitKey, out bool allowOperation);
                    if (!allowOperation)
                    {

                        //We should report the same kind of error from Middleware and the Mvc layer.
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.Response.ContentType = ContentTypes.ApplicationProblemJson;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Detail = string.Empty
                        }), context.RequestAborted);
                        return;
                    }
                }

                await _next(context);
            }
            finally
            {
                incrementor?.Dispose();
            }
        }
    }
}
