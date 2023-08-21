﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
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

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectLiveMetricsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLiveMetricsAction(_serviceProvider, processInfo, options);
        }

        private sealed class CollectLiveMetricsAction :
            CollectionRuleEgressActionBase<CollectLiveMetricsOptions>
        {
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
            private readonly IMetricsOperationFactory _metricsOperationFactory;

            public CollectLiveMetricsAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectLiveMetricsOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                _metricsOperationFactory = serviceProvider.GetRequiredService<IMetricsOperationFactory>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata collectionRuleMetadata)
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
                    operation,
                    egressProvider,
                    ProcessInfo,
                    scope,
                    null,
                    collectionRuleMetadata);

                return egressOperation;
            }
        }
    }
}
