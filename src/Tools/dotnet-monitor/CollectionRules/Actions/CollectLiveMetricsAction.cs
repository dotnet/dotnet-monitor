// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLiveMetricsActionFactory :
        ICollectionRuleActionFactory<CollectLiveMetricsOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectLiveMetricsActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectLiveMetricsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLiveMetricsAction(_serviceProvider, endpointInfo, options);
        }

        private sealed class CollectLiveMetricsAction :
            CollectionRuleActionBase<CollectLiveMetricsOptions>
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
            private readonly IMetricsOperationFactory _metricsOperationFactory;

            public CollectLiveMetricsAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectLiveMetricsOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                _metricsOperationFactory = serviceProvider.GetRequiredService<IMetricsOperationFactory>();
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CollectionRuleMetadata collectionRuleMetadata,
                CancellationToken token)
            {
                EventMetricsProvider[] providers = Options.Providers;
                EventMetricsMeter[] meters = Options.Meters;
                bool includeDefaultProviders = Options.IncludeDefaultProviders.GetValueOrDefault(CollectLiveMetricsOptionsDefaults.IncludeDefaultProviders);
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLiveMetricsOptionsDefaults.Duration));
                string egressProvider = Options.Egress;

                EventMetricsConfiguration configuration = new EventMetricsConfiguration()
                {
                    IncludeDefaultProviders = includeDefaultProviders,
                    Providers = providers,
                    Meters = meters
                };

                MetricsPipelineSettings settings = MetricsSettingsFactory.CreateSettings(
                    _counterOptions.CurrentValue,
                    (int)duration.TotalSeconds,
                    configuration);

                IArtifactOperation operation = _metricsOperationFactory.Create(
                    EndpointInfo,
                    settings);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Metrics, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    (outputStream, token) => operation.ExecuteAsync(outputStream, startCompletionSource, token),
                    egressProvider,
                    operation.GenerateFileName(),
                    EndpointInfo,
                    operation.ContentType,
                    scope,
                    collectionRuleMetadata);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string liveMetricsFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, liveMetricsFilePath }
                    }
                };
            }
        }
    }
}
