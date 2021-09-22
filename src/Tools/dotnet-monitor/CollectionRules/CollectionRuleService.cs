﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleService : IAsyncDisposable
    {
        private readonly ActionListExecutor _actionListExecutor;
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly ILogger<CollectionRuleService> _logger;
        private readonly IOptionsMonitor<CollectionRuleOptions> _optionsMonitor;
        private readonly CollectionRulesConfigurationProvider _provider;
        private readonly List<Task> _runTasks = new();
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        private bool _disposed;

        public CollectionRuleService(
            ILogger<CollectionRuleService> logger,
            CollectionRulesConfigurationProvider provider,
            ICollectionRuleTriggerOperations triggerOperations,
            ActionListExecutor actionListExecutor,
            IOptionsMonitor<CollectionRuleOptions> optionsMonitor
            )
        {
            _actionListExecutor = actionListExecutor ?? throw new ArgumentNullException(nameof(actionListExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _triggerOperations = triggerOperations ?? throw new ArgumentNullException(nameof(triggerOperations));
        }

        public async ValueTask DisposeAsync()
        {
            lock (_disposalTokenSource)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            _disposalTokenSource.SafeCancel();

            await Task.WhenAll(_runTasks.ToArray());

            _disposalTokenSource.Dispose();
        }

        public async Task ApplyRules(
            IEndpointInfo endpointInfo,
            CancellationToken token)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(CollectionRuleService).FullName);
            }

            if (null == endpointInfo)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            KeyValueLogScope scope = new();
            scope.AddCollectionRuleEndpointInfo(endpointInfo);
            // Constrain the scope of the log scope to just the log call so that the log scope
            // is not captured by the rule execution method.
            using (_logger.BeginScope(scope))
            {
                _logger.ApplyingCollectionRules();
            }

            IReadOnlyCollection<string> ruleNames = _provider.GetCollectionRuleNames();
            List<TaskCompletionSource<object>> startedSources = new(ruleNames.Count);

            // Wrap the passed CancellationToken into a linked CancellationTokenSource so that the
            // RunRuleAsync method is only cancellable for the execution of the ApplyRules method.
            // Don't want the caller to be able to cancel the run of the rules after having finished
            // executing the ApplyRules method.
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            IProcessInfo processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo);

            foreach (string ruleName in ruleNames)
            {
                TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                startedSources.Add(startedSource);

                // Start running the rule and wrap running task
                // in a safe awaitable task so that shutdown isn't
                // failed due to failing or cancelled pipelines.
                _runTasks.Add(RunRuleAsync(
                    _actionListExecutor,
                    _triggerOperations,
                    _optionsMonitor,
                    processInfo,
                    ruleName,
                    startedSource,
                    linkedSource.Token).SafeAwait());
            }

            // Wait for all started sources to complete before finishing rule application
            await Task.WhenAll(startedSources.Select(s => s.Task.SafeAwait()).ToArray());

            using (_logger.BeginScope(scope))
            {
                _logger.CollectionRulesStarted();
            }
        }

        private async Task RunRuleAsync(
            ActionListExecutor actionListExecutor,
            ICollectionRuleTriggerOperations triggerOperations,
            IOptionsMonitor<CollectionRuleOptions> optionsMonitor,
            IProcessInfo processInfo,
            string ruleName,
            TaskCompletionSource<object> startedSource,
            CancellationToken token)
        {
            KeyValueLogScope scope = new();
            scope.AddCollectionRuleEndpointInfo(processInfo.EndpointInfo);
            scope.AddCollectionRuleName(ruleName);
            using IDisposable loggerScope = _logger.BeginScope(scope);

            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                _disposalTokenSource.Token,
                token);

            try
            {
                CollectionRuleOptions options = optionsMonitor.Get(ruleName);

                if (null != options.Filters)
                {
                    DiagProcessFilter filter = DiagProcessFilter.FromConfiguration(options.Filters);

                    if (!filter.Filters.All(f => f.MatchFilter(processInfo)))
                    {
                        // Collection rule filter does not match target process
                        _logger.CollectionRuleUnmatchedFilters(ruleName);

                        // Signal rule has "started" in order to not block
                        // resumption of the runtime instance.
                        startedSource.TrySetResult(null);

                        return;
                    }
                }

                _logger.CollectionRuleStarted(ruleName);

                CollectionRuleContext context = new(ruleName, options, processInfo.EndpointInfo, _logger);

                await using CollectionRulePipeline pipeline = new(
                    actionListExecutor,
                    triggerOperations,
                    context,
                    () => startedSource.TrySetResult(null));

                await pipeline.RunAsync(linkedSource.Token);

                _logger.CollectionRuleCompleted(ruleName);
            }
            catch (OperationCanceledException ex)
            {
                startedSource.TrySetCanceled(ex.CancellationToken);

                // Rethrow if the service is not shutting down; otherwise, handle
                // the exception and allow the method to complete successfully.
                if (!_disposalTokenSource.IsCancellationRequested)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.CollectionRuleFailed(ruleName, ex);

                startedSource.TrySetException(ex);

                throw;
            }
        }
    }
}
