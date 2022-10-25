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
            _httpContext.Response.OnCompleted((_) =>
            {
                _responseFinishedCompletionSource.TrySetResult(_httpContext.Response.StatusCode);
                return Task.CompletedTask;
            }, null);

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            int statusCode;
            try
            {
                using IDisposable registration = _httpContext.RequestAborted.Register(
                    () => _responseFinishedCompletionSource.TrySetCanceled(_httpContext.RequestAborted));

                // If the http request is aborted, it will cause an OperationCanceledException here.
                // When this occurs, the operation service will mirror the cancelled state into the
                // operation store.
                statusCode = await _responseFinishedCompletionSource.Task.WaitAsync(token);
            }
            catch (ObjectDisposedException)
            {
                // If the http request is disposed by the time the operation service has a chance to get here
                // then either the response must have gracefully completed or it was aborted.
                if (_responseFinishedCompletionSource.Task.IsCompleted)
                {
                    statusCode = await _responseFinishedCompletionSource.Task;
                }
                else
                {
                    throw new OperationCanceledException("The HTTP request was aborted before the operation could be completed.");
                }
            }

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
