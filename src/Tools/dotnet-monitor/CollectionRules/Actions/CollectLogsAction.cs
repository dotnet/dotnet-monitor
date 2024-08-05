// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLogsActionFactory :
        ICollectionRuleActionFactory<CollectLogsOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectLogsActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectLogsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLogsAction(_serviceProvider, processInfo, options);
        }

        private sealed class CollectLogsAction :
            CollectionRuleEgressActionBase<CollectLogsOptions>
        {
            public CollectLogsAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectLogsOptions options)
                : base(serviceProvider, processInfo, options)
            {
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLogsOptionsDefaults.Duration));
                bool useAppFilters = Options.UseAppFilters.GetValueOrDefault(CollectLogsOptionsDefaults.UseAppFilters);
                LogLevel defaultLevel = Options.DefaultLevel.GetValueOrDefault(CollectLogsOptionsDefaults.DefaultLevel);
                Dictionary<string, LogLevel?>? filterSpecs = Options.FilterSpecs;
                LogFormat logFormat = Options.Format.GetValueOrDefault(CollectLogsOptionsDefaults.Format);

                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration,
                    LogLevel = defaultLevel,
                    UseAppFilters = useAppFilters,
                    FilterSpecs = filterSpecs
                };

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Logs, EndpointInfo);

                ILogsOperationFactory operationFactory = ServiceProvider.GetRequiredService<ILogsOperationFactory>();

                IArtifactOperation operation = operationFactory.Create(
                    EndpointInfo,
                    settings,
                    logFormat);

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
