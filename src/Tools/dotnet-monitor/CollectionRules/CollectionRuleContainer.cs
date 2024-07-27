// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    /// <summary>
    /// Holds all of the running collection rules for the associated process.
    /// </summary>
    internal class CollectionRuleContainer : IAsyncDisposable
    {
        private readonly ActionListExecutor _actionListExecutor;
        private readonly ILogger<CollectionRuleService> _logger;
        private readonly IProcessInfo _processInfo;
        private readonly HostInfo _hostInfo;
        private readonly IOptionsMonitor<CollectionRuleOptions> _optionsMonitor;
        private readonly List<Task> _runTasks = new();
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        public List<CollectionRulePipeline> Pipelines { get; set; } = new();

        private long _disposalState;
        private CancellationTokenSource? _shutdownTokenSource;

        public CollectionRuleContainer(
            IServiceProvider serviceProvider,
            ILogger<CollectionRuleService> logger,
            IProcessInfo processInfo)
        {
            if (null == serviceProvider)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));

            _hostInfo = HostInfo.GetCurrent(serviceProvider.GetRequiredService<TimeProvider>());
            _actionListExecutor = serviceProvider.GetRequiredService<ActionListExecutor>();
            _optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();
            _triggerOperations = serviceProvider.GetRequiredService<ICollectionRuleTriggerOperations>();
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
            {
                return;
            }

            await StopRulesCore(CancellationToken.None);
        }

        public async Task StartRulesAsync(
            IReadOnlyCollection<string> ruleNames,
            CancellationToken token)
        {
            DisposableHelper.ThrowIfDisposed<CollectionRuleContainer>(ref _disposalState);

            Debug.Assert(null == _shutdownTokenSource, "Collection rules were previously started without stopping.");

            _shutdownTokenSource = new();

            KeyValueLogScope logScope = new();
            logScope.AddCollectionRuleEndpointInfo(_processInfo.EndpointInfo);
            // Constrain the scope of the log scope to just the log call so that the log scope
            // is not captured by the rule execution method.
            using (_logger.BeginScope(logScope))
            {
                _logger.CollectionRulesStarting();
            }

            List<Task> startTasks = new List<Task>(ruleNames.Count);
            foreach (string ruleName in ruleNames)
            {
                // Start running the rule and wrap running task
                // in a safe awaitable task so that shutdown isn't
                // failed due to failing or cancelled pipelines.
                startTasks.Add(StartRuleAsync(ruleName, token).SafeAwait());
            }

            // Wait for all start tasks to complete before finishing rule application
            await Task.WhenAll(startTasks);

            using (_logger.BeginScope(logScope))
            {
                _logger.CollectionRulesStarted();
            }
        }

        public async Task StopRulesAsync(
            CancellationToken token)
        {
            DisposableHelper.ThrowIfDisposed<CollectionRuleContainer>(ref _disposalState);

            KeyValueLogScope logScope = new();
            logScope.AddCollectionRuleEndpointInfo(_processInfo.EndpointInfo);
            // Constrain the scope of the log scope to just the log call so that the log scope
            // is not captured by the rule execution method.
            using (_logger.BeginScope(logScope))
            {
                _logger.CollectionRulesStopping();
            }

            await StopRulesCore(token);

            using (_logger.BeginScope(logScope))
            {
                _logger.CollectionRulesStopped();
            }
        }

        /// <summary>
        /// Start a collection rule for the process associated with the container.
        /// </summary>
        /// <returns>
        /// A task that is completed when the collection rule has started.
        /// </returns>
        private async Task StartRuleAsync(
            string ruleName,
            CancellationToken token)
        {
            // Wrap the passed CancellationToken into a linked CancellationTokenSource so that the
            // RunRuleAsync method is only cancellable for the execution of the ApplyRules method.
            // Don't want the caller to be able to cancel the run of the rules after having finished
            // executing the ApplyRules method.
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            TaskCompletionSource<object?> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            // Start running the rule and wrap running task
            // in a safe awaitable task so that shutdown isn't
            // failed due to failing or cancelled pipelines.
            _runTasks.Add(RunRuleAsync(
                ruleName,
                startedSource,
                linkedSource.Token).SafeAwait());

            await startedSource.Task;
        }

        /// <summary>
        /// Run the collection rule on the process associated with the container.
        /// </summary>
        /// <returns>
        /// A task that is completed when the collection rule has run to completion.
        /// </returns>
        /// <remarks>
        /// This method will complete the <paramref name="startedSource"/> parameter
        /// when the collection rule has successfully started.
        /// </remarks>
        private async Task RunRuleAsync(
            string ruleName,
            TaskCompletionSource<object?> startedSource,
            CancellationToken token)
        {
            KeyValueLogScope scope = new();
            scope.AddCollectionRuleEndpointInfo(_processInfo.EndpointInfo);
            scope.AddCollectionRuleName(ruleName);
            using IDisposable? loggerScope = _logger.BeginScope(scope);

#nullable disable
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                _shutdownTokenSource.Token,
                token);
#nullable restore
            try
            {
                CollectionRuleOptions options = _optionsMonitor.Get(ruleName);

                DiagProcessFilter filter = DiagProcessFilter.FromConfiguration(options.Filters);

                if (!filter.Filters.All(f => f.MatchFilter(_processInfo)))
                {
                    // Collection rule filter does not match target process
                    _logger.CollectionRuleUnmatchedFilters(ruleName);

                    // Signal rule has "started" in order to not block
                    // resumption of the runtime instance.
                    startedSource.TrySetResult(null);

                    return;
                }

                _logger.CollectionRuleStarted(ruleName);

                CollectionRuleContext context = new(ruleName, options, _processInfo, _hostInfo, _logger);

                await using CollectionRulePipeline pipeline = new(
                    _actionListExecutor,
                    _triggerOperations,
                    context,
                    () => startedSource.TrySetResult(null));

                Pipelines.Add(pipeline);

                await pipeline.RunAsync(linkedSource.Token);

                _logger.CollectionRuleCompleted(ruleName);
            }
            catch (OperationCanceledException ex) when (TrySetCanceledAndReturnTrue(ex, startedSource))
            {
                if (!_shutdownTokenSource.IsCancellationRequested)
                {
                    _logger.CollectionRuleCancelled(ruleName);
                }
            }
            catch (Exception ex) when (LogExceptionAndReturnFalse(ex, startedSource, ruleName))
            {
                throw;
            }
        }

        private async Task StopRulesCore(CancellationToken token)
        {
            if (null != _shutdownTokenSource)
            {
                _shutdownTokenSource.SafeCancel();

                await Task.WhenAll(_runTasks.ToArray()).WithCancellation(token);

                _runTasks.Clear();

                _shutdownTokenSource.Dispose();

                _shutdownTokenSource = null;
            }
        }

        private static bool TrySetCanceledAndReturnTrue(OperationCanceledException ex, TaskCompletionSource<object?> source)
        {
            // Always attempt to cancel the completion source
            source.TrySetCanceled(ex.CancellationToken);

            // Always handle the exception
            return true;
        }

        private bool LogExceptionAndReturnFalse(Exception ex, TaskCompletionSource<object?> source, string ruleName)
        {
            // Log failure
            _logger.CollectionRuleFailed(ruleName, ex);

            // Always attempt to fail the completion source
            source.TrySetException(ex);

            // Never handle the exception
            return false;
        }
    }
}
