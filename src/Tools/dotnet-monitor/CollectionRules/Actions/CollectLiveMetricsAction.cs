// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLiveMetricsActionFactory :
        ICollectionRuleActionFactory<CollectLiveMetricsOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ValidationOptions _validationOptions;

        public CollectLiveMetricsActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectLiveMetricsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationHelper.ValidateObject(options, typeof(CollectLiveMetricsOptions), _validationOptions, _serviceProvider);

            return new CollectLiveMetricsAction(_serviceProvider, processInfo, options);
        }

        private sealed class CollectLiveMetricsAction :
            CollectionRuleEgressActionBase<CollectLiveMetricsOptions>
        {
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
            private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;
            private readonly IMetricsOperationFactory _metricsOperationFactory;

            public CollectLiveMetricsAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectLiveMetricsOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                _metricsOperationFactory = serviceProvider.GetRequiredService<IMetricsOperationFactory>();
                _metricsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsOptions>>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
            {
                MetricsPipelineSettings settings;
                if (Options.HasCustomConfiguration())
                {
                    EventMetricsConfiguration configuration = new()
                    {
                        IncludeDefaultProviders = Options.GetIncludeDefaultProviders(),
                        Providers = Options.Providers,
                        Meters = Options.Meters
                    };

                    settings = MetricsSettingsFactory.CreateSettings(
                        _counterOptions.CurrentValue,
                        (int)Options.GetDuration().TotalSeconds,
                        configuration);
                }
                else
                {
                    settings = MetricsSettingsFactory.CreateSettings(
                        _counterOptions.CurrentValue,
                        (int)Options.GetDuration().TotalSeconds,
                        _metricsOptions.CurrentValue);
                }

                IArtifactOperation operation = _metricsOperationFactory.Create(
                    EndpointInfo,
                    settings);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Metrics, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    operation,
                    Options.Egress,
                    Options.ArtifactName,
                    ProcessInfo,
                    scope,
                    null,
                    collectionRuleMetadata);

                return egressOperation;
            }
        }
    }

    internal sealed class CollectLiveMetricsActionDescriptor : ICollectionRuleActionDescriptor
    {
        public string ActionName => KnownCollectionRuleActions.CollectLiveMetrics;
        public Type FactoryType => typeof(CollectLiveMetricsActionFactory);
        public Type OptionsType => typeof(CollectLiveMetricsOptions);

        public void BindOptions(IConfigurationSection settingsSection, out object settings)
        {
            CollectLiveMetricsOptions options = new();
            settingsSection.Bind(options);
            settings = options;
        }
    }
}
