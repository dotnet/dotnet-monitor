// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectDumpOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectDumpAction(_serviceProvider, endpointInfo, options);
        }

        internal sealed class CollectDumpAction :
            CollectionRuleActionBase<CollectDumpOptions>
        {
            private readonly IDumpOperationFactory _dumpOperationFactory;
            private readonly IServiceProvider _serviceProvider;

            public CollectDumpAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectDumpOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _dumpOperationFactory = serviceProvider.GetRequiredService<IDumpOperationFactory>();
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CollectionRuleMetadata collectionRuleMetadata,
                CancellationToken token)
            {
                DumpType dumpType = Options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
                string egressProvider = Options.Egress;

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Dump, EndpointInfo);

                IArtifactOperation dumpOperation = _dumpOperationFactory.Create(EndpointInfo, dumpType);

                EgressOperation egressOperation = new EgressOperation(
                    (outputStream, token) => dumpOperation.ExecuteAsync(outputStream, startCompletionSource, token),
                    egressProvider,
                    dumpOperation.GenerateFileName(),
                    EndpointInfo,
                    dumpOperation.ContentType,
                    scope,
                    collectionRuleMetadata);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string dumpFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, dumpFilePath }
                    }
                };
            }
        }
    }
}
