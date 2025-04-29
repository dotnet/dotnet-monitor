// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectExceptionsActionFactory : ICollectionRuleActionFactory<CollectExceptionsOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ValidationOptions _validationOptions;
    
        public CollectExceptionsActionFactory(IServiceProvider serviceProvider, IOptions<ValidationOptions> validationOptions)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validationOptions = validationOptions?.Value ?? throw new ArgumentNullException(nameof(validationOptions));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectExceptionsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationHelper.ValidateObject(options, typeof(CollectExceptionsOptions), _validationOptions, _serviceProvider);

            return new CollectExceptionsAction(processInfo, options);
        }
    }

    internal sealed class CollectExceptionsAction :
        CollectionRuleEgressActionBase<CollectExceptionsOptions>
    {
        private readonly IExceptionsOperationFactory _operationFactory;

        public CollectExceptionsAction(IProcessInfo processInfo, CollectExceptionsOptions options)
            : base(processInfo.EndpointInfo.ServiceProvider, processInfo, options)
        {
            _operationFactory = ServiceProvider.GetRequiredService<IExceptionsOperationFactory>();
        }

        protected override EgressOperation CreateArtifactOperation(CollectionRuleMetadata? collectionRuleMetadata)
        {
            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Exceptions, EndpointInfo);

            IArtifactOperation operation = _operationFactory.Create(Options.GetFormat(), Options.GetConfigurationSettings());

            EgressOperation egressOperation = new EgressOperation(
                operation,
                Options.Egress,
                Options.ArtifactName,
                ProcessInfo,
                scope,
                tags: null,
                collectionRuleMetadata: collectionRuleMetadata);

            return egressOperation;
        }
    }

    internal sealed class CollectExceptionsActionDescriptor : ICollectionRuleActionDescriptor
    {
        public string ActionName => KnownCollectionRuleActions.CollectExceptions;
        public Type FactoryType => typeof(CollectExceptionsActionFactory);
        public Type OptionsType => typeof(CollectExceptionsOptions);

        public void BindOptions(IConfigurationSection settingsSection, out object settings)
        {
            CollectExceptionsOptions options = new();
            settingsSection.Bind(options);
            settings = options;
        }
    }
}
