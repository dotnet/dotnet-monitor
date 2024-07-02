// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ClientEndpointInfoSource : IEndpointInfoSourceInternal
    {
        // The amount of time to wait before abandoning the attempt to create an EndpointInfo from
        // the enumerated processes. This may happen if a runtime instance is unresponsive to
        // diagnostic pipe commands. Give a generous amount of time, but not too long since a single
        // unresponsive process will cause all HTTP requests to be delayed by the timeout period.
        private static readonly TimeSpan AbandonProcessTimeout = TimeSpan.FromSeconds(3);

        private readonly ILogger<ClientEndpointInfoSource> _logger;

        public ClientEndpointInfoSource(ILogger<ClientEndpointInfoSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token)
        {
            using CancellationTokenSource timeoutTokenSource = new();
            using CancellationTokenSource linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token);

            CancellationToken timeoutToken = timeoutTokenSource.Token;
            CancellationToken linkedToken = linkedTokenSource.Token;

            var endpointInfoTasks = new List<Task<EndpointInfo?>>();
            // Run the EndpointInfo creation parallel. The call to FromProcessId sends
            // a GetProcessInfo command to the runtime instance to get additional information.
            foreach (int pid in DiagnosticsClient.GetPublishedProcesses())
            {
                endpointInfoTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await EndpointInfo.FromProcessIdAsync(pid, new NotSupportedServiceProvider(), linkedToken);
                    }
                    // Catch when timeout on waiting for EndpointInfo creation. Some runtime instances may be
                    // in a bad state and not respond to any requests on their diagnostic pipe; gracefully abandon
                    // waiting for these processes.
                    catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
                    {
                        _logger.DiagnosticRequestCancelled(pid);
                        return null;
                    }
                    // Catch all other exceptions and log them.
                    catch (Exception ex)
                    {
                        _logger.DiagnosticRequestFailed(pid, ex);
                        return null;
                    }
                }, linkedToken));
            }

            timeoutTokenSource.CancelAfter(AbandonProcessTimeout);

            await Task.WhenAll(endpointInfoTasks);

            return endpointInfoTasks.Where(t => t.Result != null).Select(t => t.Result).Cast<IEndpointInfo>();
        }
    }
}
