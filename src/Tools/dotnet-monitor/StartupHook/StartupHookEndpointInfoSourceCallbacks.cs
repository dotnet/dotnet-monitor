// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
    {
        private readonly StartupHookValidator _startupHookValidator;


        public StartupHookEndpointInfoSourceCallbacks(StartupHookValidator startupHookValidator)
        {
            _startupHookValidator = startupHookValidator;
        }

        Task IEndpointInfoSourceCallbacks.OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return _startupHookValidator.CheckAsync(endpointInfo, cancellationToken, logInstructions: true);
        }

        Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
