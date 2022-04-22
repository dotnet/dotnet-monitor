using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class RequestLimitTracker
    {
        private sealed class RequestCount : IDisposable
        {
            private int _count = 0;
            public int Increment() => Interlocked.Increment(ref _count);

            public void Decrement() => Interlocked.Decrement(ref _count);

            public void Dispose() => Decrement();
        }

        private readonly Dictionary<string, int> _requestLimitTable = new();
        private readonly ConcurrentDictionary<string, RequestCount> _requestCounts = new();
        private readonly ILogger<RequestLimitTracker> _logger;

        public RequestLimitTracker(ILogger<RequestLimitTracker> logger)
        {
            //CONSIDER Should we have configuration for these?

            _requestLimitTable.Add(Utilities.ArtifactType_Dump, 1);
            _requestLimitTable.Add(Utilities.ArtifactType_GCDump, 1);
            _requestLimitTable.Add(Utilities.ArtifactType_Logs, 3);
            _requestLimitTable.Add(Utilities.ArtifactType_Trace, 3);
            _requestLimitTable.Add(Utilities.ArtifactType_Metrics, 3);
            _requestLimitTable.Add(Utilities.ArtifactType_Stacks, 3);

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
