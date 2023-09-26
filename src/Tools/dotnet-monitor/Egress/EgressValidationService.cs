// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            _egressProviderSource.Initialize();

            SemaphoreSlim semaphore = new(initialCount: 0, maxCount: 1);

            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                using CancellationTokenSource configChangedCancellationSource = new();
                using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    configChangedCancellationSource.Token,
                    token);
                CancellationToken linkedToken = linkedSource.Token;

                EventHandler configChangedHandler = (s, e) =>
                {
                    try
                    {
                        configChangedCancellationSource.SafeCancel();
                        semaphore.Release();
                    }
                    catch (SemaphoreFullException)
                    {
                        // Ignore since multiple reloads can overflow it
                    }
                };

                try
                {
                    _egressProviderSource.ProvidersChanged += configChangedHandler;
                    IReadOnlyCollection<string> providerNames = _egressProviderSource.ProviderNames;
                    List<Task> validationTasks = new(providerNames.Count);
                    foreach (var providerName in providerNames)
                    {
                        validationTasks.Add(Task.Run(() => EgressOperation.ValidateAsync(_serviceProvider, providerName, linkedToken), linkedToken).SafeAwait());
                    }
                    await Task.WhenAll(validationTasks);
                }
                catch (OperationCanceledException) when (configChangedCancellationSource.IsCancellationRequested)
                {
                    // Catch exception if due to configuration change.
                }
                finally
                {
                    await semaphore.WaitAsync(token);

                    _egressProviderSource.ProvidersChanged -= configChangedHandler;
                }
            }
        }
    }
}
