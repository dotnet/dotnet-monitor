// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class HttpResponseEgressOperation : IEgressOperation
    {
        private readonly HttpContext _httpContext;
        private readonly TaskCompletionSource<int> _responseFinishedCompletionSource = new();

        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get { return null; } }

        public HttpResponseEgressOperation(HttpContext context, IProcessInfo processInfo)
        {
            _httpContext = context;
            _httpContext.Response.OnCompleted(() =>
            {
                _responseFinishedCompletionSource.TrySetResult(_httpContext.Response.StatusCode);
                return Task.CompletedTask;
            });

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            using CancellationTokenSource cancellationTokenSourc = CancellationTokenSource.CreateLinkedTokenSource(token, _httpContext.RequestAborted);
            using IDisposable registration = token.Register(_httpContext.Abort);

            int statusCode = await _responseFinishedCompletionSource.Task.WaitAsync(cancellationTokenSourc.Token);

            return statusCode == (int)HttpStatusCode.OK
                ? ExecutionResult<EgressResult>.Empty()
                : ExecutionResult<EgressResult>.Failed(new Exception($"HTTP request failed with status code: ${statusCode}"));
        }

        public void Validate(IServiceProvider serviceProvider)
        {
            // noop
        }
    }
}
