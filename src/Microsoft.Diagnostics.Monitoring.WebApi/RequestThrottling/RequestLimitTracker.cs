// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class RequestLimitTracker : IRequestLimitTracker
    {
        public static readonly string Unlimited = string.Empty;

        private sealed class RequestCount : IDisposable
        {
            private int _count;
            public int Increment() => Interlocked.Increment(ref _count);

            public void Decrement() => Interlocked.Decrement(ref _count);

            public void Dispose() => Decrement();
        }

        private readonly Dictionary<string, int> _requestLimitTable = new();
        private readonly ConcurrentDictionary<string, RequestCount> _requestCounts = new();
        private readonly ILogger<RequestLimitTracker> _logger;

        public RequestLimitTracker(ILogger<RequestLimitTracker> logger, IEnumerable<RequestLimit> limits)
        {
            foreach (RequestLimit requestLimit in limits)
            {
                _requestLimitTable.Add(requestLimit.Key, requestLimit.Limit);
            }


            _logger = logger;
        }

        public IDisposable Increment(string key, out bool allowOperation)
        {
            if (!_requestLimitTable.TryGetValue(key, out int maxConcurrency))
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings.ErrorMessage_UnexpectedLimitKey, key));
            }

            RequestCount requestCount = _requestCounts.GetOrAdd(key, (_) => new RequestCount());
            int newRequestCount = requestCount.Increment();
            if (newRequestCount > maxConcurrency)
            {
                _logger.ThrottledEndpoint(maxConcurrency, newRequestCount);
                allowOperation = false;
            }
            else
            {
                allowOperation = true;
            }

            return requestCount;
        }
    }
}
