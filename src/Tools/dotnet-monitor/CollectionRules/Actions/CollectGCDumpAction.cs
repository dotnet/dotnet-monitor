// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectGCDumpActionFactory :
        ICollectionRuleActionFactory<CollectGCDumpOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectGCDumpActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectGCDumpOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectGCDumpAction(_serviceProvider, processInfo, options);
        }

        private sealed class CollectGCDumpAction :
            CollectionRuleEgressActionBase<CollectGCDumpOptions>
        {
            private readonly IGCDumpOperationFactory _operationFactory;

            public CollectGCDumpAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectGCDumpOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _operationFactory = serviceProvider.GetRequiredService<IGCDumpOperationFactory>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
            {
                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_GCDump, EndpointInfo);

                return new EgressOperation(
                    _operationFactory.Create(ProcessInfo.EndpointInfo),
                    Options.Egress,
                    Options.ArtifactName,
                    ProcessInfo,
                    scope,
                    tags: null,
                    collectionRuleMetadata);
            }
        }
    }

    [OptionsValidator]
    internal sealed partial class CollectGCDumpActionDescriptor : ICollectionRuleActionDescriptor<CollectGCDumpOptions, CollectGCDumpActionFactory>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectGCDumpActionDescriptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string ActionName => KnownCollectionRuleActions.CollectGCDump;

        public void BindOptions(IConfigurationSection settingsSection, out CollectGCDumpOptions options)
        {
            options = new();
            settingsSection.Bind(options);
        }
    }
}
