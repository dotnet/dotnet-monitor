// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal class EgressValidationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private EgressProviderSource _egressProviderSource;
        private List<Task> _validationTasks = new();

        public EgressValidationService(IServiceProvider serviceProvider, EgressProviderSource source)
        {
            _serviceProvider = serviceProvider;
            _egressProviderSource = source;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            // Indicates that rules changed while validating a previous change.
            // Used to indicate that the next iteration should not wait for
            // another configuration change since a change was already detected.
            bool configurationChanged = false;

            bool initialLoad = true;

            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                if (!_egressProviderSource.ProviderNames.IsNullOrEmpty() && initialLoad)
                {
                    // Guarantees we do an initial validation
                }
                else if (!configurationChanged)
                {
                    using EventTaskSource<EventHandler> configurationChangedTaskSource = new(
                        callback => (s, e) => callback(),
                        handler => _egressProviderSource.ConfigurationChanged += handler,
                        handler => _egressProviderSource.ConfigurationChanged -= handler,
                        token);

                    // Wait for the rules to be changed
                    await configurationChangedTaskSource.Task;
                }

                using CancellationTokenSource configChangedCancellationSource = new();
                using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    configChangedCancellationSource.Token,
                    token);
                CancellationToken linkedToken = linkedSource.Token;

                configurationChanged = false;

                EventHandler configChangedHandler = (s, e) =>
                {
                    // Remember that the rules were changed (so that the next iteration doesn't
                    // have to wait for them to change again) and signal cancellation due to the
                    // change.
                    configurationChanged = true;
                    configChangedCancellationSource.SafeCancel();
                };

                try
                {
                    _egressProviderSource.ConfigurationChanged += configChangedHandler;

                    lock(_egressProviderSource.ProviderNames)
                    {
                        foreach (var providerName in _egressProviderSource.ProviderNames)
                        {
                            _validationTasks.Add(Task.Run(() => EgressOperation.ValidateAsync(_serviceProvider, providerName, token).SafeAwait(), token));
                        }
                    }

                    Task.WaitAll(_validationTasks.ToArray(), linkedToken);

                }
                catch (OperationCanceledException) when (configChangedCancellationSource.IsCancellationRequested)
                {
                    // Catch exception if due to rule change.
                }
                finally
                {
                    _validationTasks.Clear();

                    _egressProviderSource.ConfigurationChanged -= configChangedHandler;
                }
            }
        }
    }
}
