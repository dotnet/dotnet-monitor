// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
    {
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly StartupHookValidator _startupHookValidator;


        public StartupHookEndpointInfoSourceCallbacks(
            IInProcessFeatures inProcessFeatures,
            StartupHookValidator startupHookValidator)
        {
            _inProcessFeatures = inProcessFeatures;
            _startupHookValidator = startupHookValidator;
        }

        Task IEndpointInfoSourceCallbacks.OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            if (_inProcessFeatures.IsStartupHookRequired)
            {
                if (_startupHookValidator.CheckEnvironmentAsync(endpointInfo, cancellationToken).Result)
                {
                    return Task.CompletedTask;
                }

                if (_startupHookValidator.ApplyStartupHook(endpointInfo, cancellationToken).Result)
                {
                    return Task.CompletedTask;
                }

                return _startupHookValidator.CheckEnvironmentAsync(endpointInfo, cancellationToken, logInstructions: true);
            }

            return Task.CompletedTask;
        }

        Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
