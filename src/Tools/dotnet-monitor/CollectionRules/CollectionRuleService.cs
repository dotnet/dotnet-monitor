// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleService : IAsyncDisposable
    {
        private readonly List<CollectionRuleContainer> _containers = new();
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
        }

        public async ValueTask DisposeAsync()
        {
            if (DisposableHelper.CanDispose(ref _disposalState))
            {
                foreach (CollectionRuleContainer container in _containers)
                {
                    await container.DisposeAsync();
                }
                _containers.Clear();
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

            KeyValueLogScope logScope = new();
            logScope.AddCollectionRuleEndpointInfo(endpointInfo);
            // Constrain the scope of the log scope to just the log call so that the log scope
            // is not captured by the rule execution method.
            using (_logger.BeginScope(logScope))
            {
                _logger.ApplyingCollectionRules();
            }

            IReadOnlyCollection<string> ruleNames = _provider.GetCollectionRuleNames();

            IProcessInfo processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo);

            CollectionRuleContainer container = new(
                _serviceProvider,
                _logger,
                processInfo);
            _containers.Add(container);

            List<Task> startTasks = new List<Task>(ruleNames.Count);
            foreach (string ruleName in ruleNames)
            {
                // Start running the rule and wrap running task
                // in a safe awaitable task so that shutdown isn't
                // failed due to failing or cancelled pipelines.
                startTasks.Add(container.StartRuleAsync(ruleName, token).SafeAwait());
            }

            // Wait for all start tasks to complete before finishing rule application
            await Task.WhenAll(startTasks);

            using (_logger.BeginScope(logScope))
            {
                _logger.CollectionRulesStarted();
            }
        }
    }
}
