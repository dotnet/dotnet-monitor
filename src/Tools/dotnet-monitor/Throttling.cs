// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private sealed class RequestCount
        {
            private int _count = 0;
            public int Increment() => Interlocked.Increment(ref _count);

            public void Decrement() => Interlocked.Decrement(ref _count);
        }

        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, RequestCount> _requestCounts = new ConcurrentDictionary<string, RequestCount>();
        private readonly ILogger<Throttling> _logger;

        public Throttling(RequestDelegate next, ILogger<Throttling> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            RequestLimitAttribute requestLimit = endpoint?.Metadata.GetMetadata<RequestLimitAttribute>();
            RequestCount requestCount = null;

            try
            {
                if (requestLimit != null)
                {
                    requestCount = _requestCounts.GetOrAdd(endpoint.DisplayName, (_) => new RequestCount());
                    int newRequestCount = requestCount.Increment();
                    if (newRequestCount > requestLimit.MaxConcurrency)
                    {
                        _logger.ThrottledEndpoint(requestLimit.MaxConcurrency, newRequestCount);
                        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                        return;
                    }
                }

                await _next(context);
            }
            finally
            {
                //IMPORTANT This will not work for operation style apis, such as when returning a 202 for egress calls.
                requestCount?.Decrement();
            }
        }
    }
}
