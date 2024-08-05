// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectStacksActionFactory : ICollectionRuleActionFactory<CollectStacksOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectStacksActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectStacksOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectStacksAction(_serviceProvider, processInfo, options);
        }
    }

    internal sealed class CollectStacksAction :
        CollectionRuleEgressActionBase<CollectStacksOptions>
    {
        private readonly IStacksOperationFactory _operationFactory;

        public CollectStacksAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectStacksOptions options)
            : base(serviceProvider, processInfo, options)
        {
            _operationFactory = serviceProvider.GetRequiredService<IStacksOperationFactory>();
        }

        protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
        {
            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Stacks, EndpointInfo);

            IArtifactOperation stacksOperation = _operationFactory.Create(EndpointInfo, MapCallStackFormat(Options.GetFormat()));

            EgressOperation egressOperation = new EgressOperation(
                stacksOperation,
                Options.Egress,
                Options.ArtifactName,
                ProcessInfo,
                scope,
                tags: null,
                collectionRuleMetadata: collectionRuleMetadata);

            return egressOperation;
        }

        private static StackFormat MapCallStackFormat(CallStackFormat format) =>
            format switch
            {
                CallStackFormat.Json => StackFormat.Json,
                CallStackFormat.PlainText => StackFormat.PlainText,
                CallStackFormat.Speedscope => StackFormat.Speedscope,
                _ => throw new InvalidOperationException()
            };
    }
}
