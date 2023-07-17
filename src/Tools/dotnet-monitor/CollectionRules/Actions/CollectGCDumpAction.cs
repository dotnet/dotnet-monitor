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
            private readonly OperationTrackerService _operationTrackerService;

            public CollectGCDumpAction(IServiceProvider serviceProvider, IProcessInfo processInfo, CollectGCDumpOptions options)
                : base(serviceProvider, processInfo, options)
            {
                _operationTrackerService = serviceProvider.GetRequiredService<OperationTrackerService>();
            }

            protected override EgressOperation CreateArtifactOperation(TaskCompletionSource<object> startCompletionSource, CollectionRuleMetadata collectionRuleMetadata)
            {
                string egress = Options.Egress;

                string gcdumpFileName = GCDumpUtilities.GenerateGCDumpFileName(EndpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_GCDump, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    async (stream, token) =>
                    {
                        using IDisposable operationRegistration = _operationTrackerService.Register(EndpointInfo);
                        startCompletionSource.TrySetResult(null);
                        await GCDumpUtilities.CaptureGCDumpAsync(EndpointInfo, stream, token);
                    },
                    egress,
                    gcdumpFileName,
                    EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope,
                    collectionRuleMetadata);

                return egressOperation;
            }
        }
    }
}
