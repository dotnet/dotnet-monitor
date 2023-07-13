// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
    {
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly StartupHookValidator _startupHookValidator;

        public IDictionary<Guid, bool> ApplyStartupState { get; set; } = new Dictionary<Guid, bool>();

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

        async Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            if (_inProcessFeatures.IsStartupHookRequired)
            {
                if (await _startupHookValidator.CheckEnvironmentAsync(endpointInfo, cancellationToken))
                {
                    return;
                }

                if (await _startupHookValidator.ApplyStartupHook(endpointInfo, cancellationToken))
                {
                    ApplyStartupState[endpointInfo.RuntimeInstanceCookie] = true;
                    return;
                }

                ApplyStartupState[endpointInfo.RuntimeInstanceCookie] = false;

                await _startupHookValidator.CheckEnvironmentAsync(endpointInfo, cancellationToken, logInstructions: true);
            }

            return;
        }

        Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
