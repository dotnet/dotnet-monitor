﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectExceptionsActionFactory : ICollectionRuleActionFactory<CollectExceptionsOptions>
    {
        private readonly ValidationOptions _validationOptions;
    
        public CollectExceptionsActionFactory(IOptions<ValidationOptions> validationOptions)
        {
            _validationOptions = validationOptions?.Value ?? throw new ArgumentNullException(nameof(validationOptions));
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectExceptionsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            List<ValidationResult> result = new();
            if (!ValidationHelper.TryValidateObject(options, typeof(CollectExceptionsOptions), _validationOptions, result))
            {
                throw new ValidationException(
                    string.Join(Environment.NewLine, result.ConvertAll(r => r.ErrorMessage)));
            }

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
}
