// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
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
    internal sealed class CollectGCDumpActionFactory :
        ICollectionRuleActionFactory<CollectGCDumpOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectGCDumpActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectGCDumpOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectGCDumpAction(_serviceProvider, endpointInfo, options);
        }

        private sealed class CollectGCDumpAction :
            CollectionRuleActionBase<CollectGCDumpOptions>
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly OperationTrackerService _operationTrackerService;

            public CollectGCDumpAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectGCDumpOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _operationTrackerService = _serviceProvider.GetRequiredService<OperationTrackerService>();
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompleteSource,
                CollectionRuleMetadata collectionRuleMetadata,
                CancellationToken token)
            {
                string egress = Options.Egress;

                string gcdumpFileName = GCDumpUtilities.GenerateGCDumpFileName(EndpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_GCDump, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    async (stream, token) =>
                    {
                        using IDisposable operationRegistration = _operationTrackerService.Register(EndpointInfo);
                        startCompleteSource.TrySetResult(null);
                        await GCDumpUtilities.CaptureGCDumpAsync(EndpointInfo, stream, token);
                    },
                    egress,
                    gcdumpFileName,
                    EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope,
                    collectionRuleMetadata);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string gcdumpFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, gcdumpFilePath }
                    }
                };
            }
        }
    }
}
