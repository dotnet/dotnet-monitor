﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class HttpResponseEgressOperation : IEgressOperation
    {
        private readonly HttpContext _httpContext;
        private readonly TaskCompletionSource<int> _responseFinishedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get { return null; } }
        public bool IsStoppable { get { return _operation?.IsStoppable ?? false; } }

        private readonly IArtifactOperation _operation;

        public HttpResponseEgressOperation(HttpContext context, IProcessInfo processInfo, IArtifactOperation operation = null)
        {
            _httpContext = context;
            _httpContext.Response.OnCompleted(() =>
            {
                _responseFinishedCompletionSource.TrySetResult(_httpContext.Response.StatusCode);
                return Task.CompletedTask;
            });

            _operation = operation;

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _httpContext.RequestAborted);
            using IDisposable registration = token.Register(_httpContext.Abort);

            int statusCode = await _responseFinishedCompletionSource.Task.WaitAsync(cancellationTokenSource.Token);

            return statusCode >= (int)HttpStatusCode.OK && statusCode < (int)HttpStatusCode.Ambiguous
                ? ExecutionResult<EgressResult>.Empty()
                : ExecutionResult<EgressResult>.Failed(
                    new Exception(string.Format(
                        CultureInfo.CurrentCulture,
                        Strings.ErrorMessage_HttpOperationFailed,
                        statusCode)));
        }

        public void Validate(IServiceProvider serviceProvider)
        {
            // noop
        }

        public Task StopAsync(CancellationToken token)
        {
            if (_operation == null)
            {
                throw new InvalidOperationException();
            }

            return _operation.StopAsync(token);
        }
    }
}
