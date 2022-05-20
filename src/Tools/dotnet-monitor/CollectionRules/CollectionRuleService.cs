﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
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

                foreach (IEndpointInfo key in _containersMap.Keys)
                {
                    _containersMap[key].Pipelines.Clear();
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

        public Dictionary<string, CollectionRuleDescription> GetCollectionRulesDescriptions(IEndpointInfo endpointInfo)
        {
            Dictionary<string, CollectionRuleDescription> collectionRulesDescriptions = new();

            foreach (IEndpointInfo key in _containersMap.Keys.Where(x => x == endpointInfo))
            {
                _containersMap[key].Pipelines.ForEach(pipeline => collectionRulesDescriptions.Add(pipeline.Context.Name, GetCollectionRuleDescription(pipeline)));
            }

            return collectionRulesDescriptions;
        }

        internal static CollectionRuleDescription GetCollectionRuleDescription(CollectionRulePipeline pipeline)
        {
            CollectionRuleLimitsOptions limitsOptions = pipeline.Context.Options.Limits;

            DateTime currentTime = pipeline.Context.Clock.UtcNow.UtcDateTime;
            Queue<DateTime> timestampsCopy = CollectionRulePipeline.DequeueOldTimestamps(pipeline.ExecutionTimestamps, limitsOptions?.ActionCountSlidingWindowDuration, currentTime);

            CollectionRuleStateHolder stateHolderCopy = new CollectionRuleStateHolder(pipeline.StateHolder);

            int actionCountLimit = (limitsOptions?.ActionCount).GetValueOrDefault(CollectionRuleLimitsOptionsDefaults.ActionCount);

            // This feels a bit clunky, but it should resolve the problem of the front-end modifying our stateHolder...
            if (pipeline.CheckForThrottling(actionCountLimit, timestampsCopy.Count, limitsOptions?.ActionCountSlidingWindowDuration))
            {
                stateHolderCopy.BeginThrottled();
            }
            else
            {
                stateHolderCopy.EndThrottled();
            }

            CollectionRuleDescription description = new()
            {
                State = stateHolderCopy.CurrentState,
                StateReason = stateHolderCopy.CurrentStateReason,
                LifetimeOccurrences = pipeline.AllExecutionTimestamps.Count,
                ActionCountLimit = actionCountLimit,
                SlidingWindowOccurrences = timestampsCopy.Count,
                ActionCountSlidingWindowDurationLimit = limitsOptions?.ActionCountSlidingWindowDuration,
            };

            if (description.State != CollectionRuleState.Finished)
            {
                description.SlidingWindowDurationCountdown = GetSWDCountdown(timestampsCopy, description.ActionCountSlidingWindowDurationLimit, description.ActionCountLimit, currentTime);
                description.RuleFinishedCountdown = GetRuleFinishedCountdown(pipeline.PipelineStartTime, limitsOptions?.RuleDuration, currentTime);
            }

            return description;
        }

        private static TimeSpan? GetSWDCountdown(Queue<DateTime> timestamps, TimeSpan? actionCountWindowDuration, int actionCount, DateTime currentTimestamp)
        {
            if (actionCountWindowDuration.HasValue && timestamps.Count >= actionCount)
            {
                TimeSpan countdown =  timestamps.Peek() - (currentTimestamp - actionCountWindowDuration.Value);
                return GetTruncatedPositiveTimeSpan(countdown);
            }

            return null;
        }

        private static TimeSpan? GetRuleFinishedCountdown(DateTime pipelineStartTime, TimeSpan? ruleDuration, DateTime currentTimestamp)
        {
            if (ruleDuration.HasValue)
            {
                TimeSpan countdown = ruleDuration.Value - (currentTimestamp - pipelineStartTime);
                return GetTruncatedPositiveTimeSpan(countdown);
            }

            return null;
        }

        private static TimeSpan? GetTruncatedPositiveTimeSpan(TimeSpan original)
        {
            return (original > TimeSpan.Zero) ? TimeSpan.FromSeconds((long)original.TotalSeconds) : null; // Intentionally lose millisecond precision
        }
    }
}
