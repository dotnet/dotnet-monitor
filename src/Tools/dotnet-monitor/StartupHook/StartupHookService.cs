// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookService :
        IDiagnosticLifetimeService
    {
        private readonly IEndpointInfo _endpointInfo;
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly TaskCompletionSource<bool> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly StartupHookValidator _startupHookValidator;

        public StartupHookService(
            IEndpointInfo endpointInfo,
            IInProcessFeatures inProcessFeatures,
            StartupHookValidator startupHookValidator)
        {
            _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            _inProcessFeatures = inProcessFeatures ?? throw new ArgumentNullException(nameof(inProcessFeatures));
            _startupHookValidator = startupHookValidator ?? throw new ArgumentNullException(nameof(startupHookValidator));
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_inProcessFeatures.IsStartupHookRequired)
            {
                if (await _startupHookValidator.CheckEnvironmentAsync(_endpointInfo, cancellationToken))
                {
                    _resultSource.SetResult(true);
                    return;
                }

                if (await _startupHookValidator.ApplyStartupHook(_endpointInfo, cancellationToken))
                {
                    _resultSource.SetResult(true);
                    return;
                }

                await _startupHookValidator.CheckEnvironmentAsync(_endpointInfo, cancellationToken, logInstructions: true);
            }

            _resultSource.SetResult(false);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public Task<bool> CheckHasStartupHookAsync(CancellationToken cancellationToken)
            => _resultSource.Task.WaitAsync(cancellationToken);
    }
}
