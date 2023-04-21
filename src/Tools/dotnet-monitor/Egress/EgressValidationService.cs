// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
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

        public EgressValidationService(EgressProviderSource source, IServiceProvider serviceProvider)
        {
            _egressProviderSource = source;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            // Indicates that configuration changed while validating a previous change.
            // Used to indicate that the next iteration should not wait for
            // another configuration change since a change was already detected.
            bool configurationChanged = false;
            bool initialLoad = true;

            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                if (initialLoad)
                {
                    // Guarantees we do an initial validation
                    _egressProviderSource.Initialize();
                    initialLoad = false;
                }
                else if (!configurationChanged)
                {
                    using EventTaskSource<EventHandler> configurationChangedTaskSource = new(
                        callback => (s, e) => callback(),
                        handler => _egressProviderSource.ConfigurationChanged += handler,
                        handler => _egressProviderSource.ConfigurationChanged -= handler,
                        token);

                    // Wait for the configuration to be changed
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
                    // Remember that the configuration was changed (so that the next iteration doesn't
                    // have to wait for them to change again) and signal cancellation due to the
                    // change.
                    configurationChanged = true;
                    configChangedCancellationSource.SafeCancel();
                };

                List<Task> validationTasks = new();

                try
                {
                    _egressProviderSource.ConfigurationChanged += configChangedHandler;

                    lock(_egressProviderSource.ProviderNames)
                    {
                        foreach (var providerName in _egressProviderSource.ProviderNames)
                        {
                            validationTasks.Add(Task.Run(() => EgressOperation.ValidateAsync(_serviceProvider, providerName, linkedToken), linkedToken).SafeAwait());
                        }
                    }

                    await Task.WhenAll(validationTasks);
                }
                catch (OperationCanceledException) when (configChangedCancellationSource.IsCancellationRequested)
                {
                    // Catch exception if due to configuration change.
                }
                finally
                {
                    _egressProviderSource.ConfigurationChanged -= configChangedHandler;
                }
            }
        }
    }
}
