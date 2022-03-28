// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleService : BackgroundService, IAsyncDisposable, ICollectionRuleService
    {
        // The number of items that the pending removal channel will hold before forcing
        // the writer to wait for capacity to be available.
        private const int PendingRemovalChannelCapacity = 1000;

        private readonly Dictionary<IEndpointInfo, CollectionRuleContainer> _containersMap = new();
        private readonly ChannelReader<CollectionRuleContainer> _containersToRemoveReader;
        private readonly ChannelWriter<CollectionRuleContainer> _containersToRemoveWriter;
        private readonly ILogger<CollectionRuleService> _logger;
        private readonly CollectionRulesConfigurationProvider _provider;
        private readonly IServiceProvider _serviceProvider;

        private long _disposalState;

        public CollectionRuleService(
            IServiceProvider serviceProvider,
            ILogger<CollectionRuleService> logger,
            CollectionRulesConfigurationProvider provider
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            BoundedChannelOptions containersToRemoveChannelOptions = new(PendingRemovalChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true
            };

            Channel<CollectionRuleContainer> containersToRemoveChannel =
                Channel.CreateBounded<CollectionRuleContainer>(containersToRemoveChannelOptions);
            _containersToRemoveReader = containersToRemoveChannel.Reader;
            _containersToRemoveWriter = containersToRemoveChannel.Writer;
        }

        public async ValueTask DisposeAsync()
        {
            if (DisposableHelper.CanDispose(ref _disposalState))
            {
                _containersToRemoveWriter.TryComplete();

                CollectionRuleContainer[] containers;
                lock (_containersMap)
                {
                    containers = _containersMap.Values.ToArray();
                }

                // This will cancel the background execution if
                // BackgroundService.StopAsync wasn't called.
                Dispose();

                foreach (CollectionRuleContainer container in containers)
                {
                    await container.DisposeAsync();
                }
            }
        }

        public async Task ApplyRules(
            IEndpointInfo endpointInfo,
            CancellationToken token)
        {
            DisposableHelper.ThrowIfDisposed<CollectionRuleService>(ref _disposalState);

            if (null == endpointInfo)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            IProcessInfo processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo);

            IReadOnlyCollection<string> ruleNames = _provider.GetCollectionRuleNames();

            CollectionRuleContainer container = new(
                _serviceProvider,
                _logger,
                processInfo);

            lock (_containersMap)
            {
                _containersMap.Add(endpointInfo, container);
            }

            if (ruleNames.Count > 0)
            {
                await container.StartRulesAsync(ruleNames, token);
            }
        }

        public async Task RemoveRules(
            IEndpointInfo endpointInfo,
            CancellationToken token)
        {
            CollectionRuleContainer container;
            lock (_containersMap)
            {
                if (!_containersMap.Remove(endpointInfo, out container))
                {
                    return;
                }
            }

            await _containersToRemoveWriter.WriteAsync(container, token);
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            return Task.WhenAll(
                MonitorRuleChangesAsync(token),
                StopAndDisposeContainerAsync(token));
        }

        private async Task MonitorRuleChangesAsync(CancellationToken token)
        {
            // Indicates that rules changed while handling a previous change.
            // Used to indicate that the next iteration should not wait for
            // another configuration change since a change was already detected.
            bool rulesChanged = false;

            while (!token.IsCancellationRequested)
            {
                if (!rulesChanged)
                {
                    using EventTaskSource<EventHandler> rulesChangedTaskSource = new(
                        callback => (s, e) => callback(),
                        handler => _provider.RulesChanged += handler,
                        handler => _provider.RulesChanged -= handler,
                        token);

                    // Wait for the rules to be changed
                    await rulesChangedTaskSource.Task;
                }

                rulesChanged = false;

                _logger.CollectionRuleConfigurationChanged();

                // Get a copy of the container list to avoid having to
                // lock the entire list during stop and restart of all containers.
                CollectionRuleContainer[] containers;
                lock (_containersMap)
                {
                    containers = _containersMap.Values.ToArray();
                }

                // Stop all rules for all processes
                List<Task> tasks = new(_containersMap.Count);
                foreach (CollectionRuleContainer container in containers)
                {
                    tasks.Add(Task.Run(() => container.StopRulesAsync(token), token).SafeAwait());
                }

                await Task.WhenAll(tasks);

                tasks.Clear();

                using CancellationTokenSource rulesChangedCancellationSource = new();
                using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    rulesChangedCancellationSource.Token,
                    token);
                CancellationToken linkedToken = linkedSource.Token;

                EventHandler rulesChangedHandler = (s, e) =>
                {
                    // Remember that the rules were changed (so that the next iteration doesn't
                    // have to wait for them to change again) and signal cancellation due to the
                    // change.
                    rulesChanged = true;
                    rulesChangedCancellationSource.SafeCancel();
                };

                // Apply new rules to existing processes. This can be cancelled if the rules change
                // again while attempting to apply the old rules; in this case, loop around again to
                // stop any rules that are in flight and apply the new rules again.
                try
                {
                    _provider.RulesChanged += rulesChangedHandler;

                    IReadOnlyCollection<string> ruleNames = _provider.GetCollectionRuleNames();

                    if (ruleNames.Count > 0)
                    {
                        foreach (CollectionRuleContainer container in containers)
                        {
                            tasks.Add(Task.Run(() => container.StartRulesAsync(ruleNames, linkedToken), linkedToken).SafeAwait());
                        }

                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (rulesChangedCancellationSource.IsCancellationRequested)
                {
                    // Catch exception if due to rule change.
                }
                finally
                {
                    _provider.RulesChanged -= rulesChangedHandler;

                    tasks.Clear();
                }
            }
        }

        private async Task StopAndDisposeContainerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                CollectionRuleContainer container = await _containersToRemoveReader.ReadAsync(token);

                await container.StopRulesAsync(token).SafeAwait();

                await container.DisposeAsync();
            }
        }

        public Dictionary<string, Monitoring.WebApi.Models.CollectionRules> GetStuff(ProcessKey? processKey)
        {
            if (processKey == null)
            {
                return null;
            }

            IReadOnlyCollection<string> ruleNames = _provider.GetCollectionRuleNames();

            var keysToUse = new List<IEndpointInfo>();

            var keys = _containersMap.Keys;
            foreach (var key in keys)
            {
                if (key.ProcessId == processKey.Value.ProcessId) // need to probably check other values as well
                {
                    keysToUse.Add(key);
                }
            }

            Dictionary<string, Monitoring.WebApi.Models.CollectionRules> toReturn = new();

            foreach (var keyToUse in keysToUse)
            {
                CleanUpCompletedPipelines(keyToUse);

                var container = _containersMap[keyToUse];

                foreach (var pipeline in container.pipelines)
                {
                    string ruleName = pipeline._context.Name;

                    CollectionRuleOptions options = container._optionsMonitor.Get(ruleName);

                    int allExecutions = pipeline._allExecutionTimestamps.Count;

                    if (options.Limits.ActionCountSlidingWindowDuration.HasValue) // Need a null check for limits
                    {
                        DateTime currentTimestamp = pipeline._context.Clock.UtcNow.UtcDateTime;

                        DateTime windowStartTimestamp = currentTimestamp - options.Limits.ActionCountSlidingWindowDuration.Value;
                        while (pipeline._executionTimestamps.Count > 0)
                        {
                            DateTime executionTimestamp = pipeline._executionTimestamps.Peek();
                            if (executionTimestamp < windowStartTimestamp)
                            {
                                pipeline._executionTimestamps.Dequeue();
                            }
                            else
                            {
                                // Stop clearing out previous executions
                                break;
                            }
                        }
                    }

                    int currExecutions = pipeline._executionTimestamps.Count; // We need to actively dequeue here -> since this normally only happens when a collection rule is operated on, not passively in the background.
                    CollectionRulesState state = CollectionRulesState.Running;
                    if (pipeline.actionIsInFlight)
                    {
                        state = CollectionRulesState.Collecting; // Need to have ways to check if we're paused/terminated
                    }
                    else if (pipeline._isCleanedUp)
                    {
                        state = CollectionRulesState.Finished; // Make sure this shouldn't be waiting to resume
                    } else if (options.Limits?.ActionCount <= currExecutions) // need to also use a default check here
                    {
                        state = CollectionRulesState.WaitingToResume; // Make sure this shouldn't be waiting to resume
                    }

                    Monitoring.WebApi.Models.CollectionRules currCollectionRuleInfo = new Monitoring.WebApi.Models.CollectionRules()
                    {
                        lifetimeTriggerOccurrences = allExecutions,
                        TriggerMaxOccurrences = (options.Limits?.ActionCount).GetValueOrDefault(3333), // need actual default
                        TriggerOccurrences = currExecutions,
                        State = state
                    };

                    toReturn.Add(ruleName, currCollectionRuleInfo);
                }
            }

            return toReturn;
        }

        private void CleanUpCompletedPipelines(IEndpointInfo key)
        {
            var toRemove = new List<CollectionRulePipeline>();

            var collectionRuleNamesFrequency = new Dictionary<string, int>();

            foreach (var pipeline in _containersMap[key].pipelines)
            {
                if (collectionRuleNamesFrequency.ContainsKey(pipeline._context.Name))
                {
                    collectionRuleNamesFrequency[pipeline._context.Name] += 1;
                }
                else
                {
                    collectionRuleNamesFrequency.Add(pipeline._context.Name, 1);
                }

                if (pipeline._isCleanedUp)
                {
                    toRemove.Add(pipeline);
                }
            }

            foreach (var pipeline in toRemove)
            {
                // Only remove things that have been removed due to a collection rule change, not due to actual completion
                if (collectionRuleNamesFrequency[pipeline._context.Name] > 1)
                {
                    _containersMap[key].pipelines.Remove(pipeline);
                }
            }
        }
    }
}
