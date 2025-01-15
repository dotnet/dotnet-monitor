// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectDumpActionFactory :
        ICollectionRuleActionFactory<CollectDumpOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectDumpActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectDumpOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectDumpAction(_serviceProvider, processInfo, options);
        }

        internal sealed class CollectDumpAction :
            CollectionRuleEgressActionBase<CollectDumpOptions>
        {
            private readonly IDumpOperationFactory _dumpOperationFactory;

            public CollectDumpAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectDumpOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _dumpOperationFactory = serviceProvider.GetRequiredService<IDumpOperationFactory>();
            }

            protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
            {
                DumpType dumpType = Options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Dump, EndpointInfo);

                IArtifactOperation dumpOperation = _dumpOperationFactory.Create(EndpointInfo, dumpType);

                EgressOperation egressOperation = new EgressOperation(
                    dumpOperation,
                    Options.Egress,
                    Options.ArtifactName,
                    ProcessInfo,
                    scope,
                    tags: null,
                    collectionRuleMetadata: collectionRuleMetadata);

                return egressOperation;
            }
        }
    }
}
