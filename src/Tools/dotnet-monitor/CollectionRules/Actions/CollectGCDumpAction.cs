// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
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
            ICollectionRuleAction,
            IAsyncDisposable
        {
            private readonly CancellationTokenSource _disposalTokenSource = new();
            private readonly IEndpointInfo _endpointInfo;
            private readonly CollectGCDumpOptions _options;
            private readonly IServiceProvider _serviceProvider;

            private Task<CollectionRuleActionResult> _completionTask;

            public CollectGCDumpAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectGCDumpOptions options)
            {
                _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            public async ValueTask DisposeAsync()
            {
                _disposalTokenSource.SafeCancel();

                await _completionTask.SafeAwait();

                _disposalTokenSource.Dispose();
            }

            public async Task StartAsync(CancellationToken token)
            {
                TaskCompletionSource<object> startCompleteSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                CancellationToken disposalToken = _disposalTokenSource.Token;
                _completionTask = Task.Run(() => ExecuteAsync(startCompleteSource, disposalToken), disposalToken);

                await startCompleteSource.WithCancellation(token);
            }

            public async Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
            {
                return await _completionTask.WithCancellation(token);
            }

            private async Task<CollectionRuleActionResult> ExecuteAsync(TaskCompletionSource<object> startCompleteSource, CancellationToken token)
            {
                string egress = _options.Egress;

                string gcdumpFileName = Utils.GenerateGCDumpFileName(_endpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_GCDump, _endpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    (stream, token) =>
                    {
                        startCompleteSource.TrySetResult(null);
                        return Utils.CaptureGCDumpAsync(_endpointInfo, stream, token);
                    },
                    egress,
                    gcdumpFileName,
                    _endpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

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