// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public partial class ApiClient : IDisposable
    {
        private readonly bool _disposeHttpClient;

        public ApiClient(string baseUrl)
            : this(baseUrl, new HttpClient())
        {
            _disposeHttpClient = true;
        }

        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        public async Task<ICollection<ProcessIdentifier>> GetProcessesAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
            return await GetProcessesAsync(timeoutSource.Token);
        }

        public async Task<string> GetMetricsAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
            return await GetMetricsAsync(timeoutSource.Token);
        }
    }
}
