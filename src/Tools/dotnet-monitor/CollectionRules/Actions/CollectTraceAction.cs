// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class CollectTraceActionFactory :
        ICollectionRuleActionFactory<CollectTraceOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectTraceOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectTraceAction(_serviceProvider, processInfo, options);
        }

        public CollectTraceActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private sealed class CollectTraceAction :
            CollectionRuleEgressActionBase<CollectTraceOptions>
        {
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;

            public CollectTraceAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectTraceOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTraceOptionsDefaults.Duration));

                MonitoringSourceConfiguration configuration;

                TraceEventFilter? stoppingEvent = null;

                if (Options.Profile.HasValue)
                {
                    TraceProfile profile = Options.Profile.Value;
                    configuration = TraceUtilities.GetTraceConfiguration(profile, _counterOptions.CurrentValue);
                }
                else
                {
#nullable disable
                    EventPipeProvider[] optionsProviders = Options.Providers.ToArray();
#nullable restore
                    bool requestRundown = Options.RequestRundown.GetValueOrDefault(CollectTraceOptionsDefaults.RequestRundown);
                    int bufferSizeMegabytes = Options.BufferSizeMegabytes.GetValueOrDefault(CollectTraceOptionsDefaults.BufferSizeMegabytes);
                    configuration = TraceUtilities.GetTraceConfiguration(optionsProviders, requestRundown, bufferSizeMegabytes);

                    stoppingEvent = Options.StoppingEvent;
                }

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Trace, EndpointInfo);

                ITraceOperationFactory operationFactory = ServiceProvider.GetRequiredService<ITraceOperationFactory>();
                IArtifactOperation operation;
                if (stoppingEvent == null)
                {
                    operation = operationFactory.Create(
                        EndpointInfo,
                        configuration,
                        duration);
                }
                else
                {
                    operation = operationFactory.Create(
                        EndpointInfo,
                        configuration,
                        duration,
                        stoppingEvent.ProviderName,
                        stoppingEvent.EventName,
                        stoppingEvent.PayloadFilter);
                }

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
}
