// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal const string EgressPathOutputValueName = "EgressPath";

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
            ICollectionRuleAction,
            IAsyncDisposable
        {
            private readonly CancellationTokenSource _disposalTokenSource = new();
            private readonly IDumpService _dumpService;
            private readonly IEndpointInfo _endpointInfo;
            private readonly CollectDumpOptions _options;
            private readonly IServiceProvider _serviceProvider;

            private Task<CollectionRuleActionResult> _completionTask;

            public CollectDumpAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectDumpOptions options)
            {
                _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _dumpService = serviceProvider.GetRequiredService<IDumpService>();
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

            private async Task<CollectionRuleActionResult> ExecuteAsync(TaskCompletionSource<object> startCompletionSource, CancellationToken token)
            {
                try
                {
                    DumpType dumpType = _options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
                    string egressProvider = _options.Egress;

                    string dumpFileName = Utils.GenerateDumpFileName();

                    string dumpFilePath = string.Empty;

                    KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Dump, _endpointInfo);

                    try
                    {
                        EgressOperation egressOperation = new EgressOperation(
                            token => {
                                startCompletionSource.TrySetResult(null);
                                return _dumpService.DumpAsync(_endpointInfo, dumpType, token);
                            },
                            egressProvider,
                            dumpFileName,
                            _endpointInfo,
                            ContentTypes.ApplicationOctetStream,
                            scope);

                        ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

                        dumpFilePath = result.Result.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new CollectionRuleActionException(ex);
                    }

                    return new CollectionRuleActionResult()
                    {
                        OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { CollectionRuleActionConstants.EgressPathOutputValueName, dumpFilePath }
                        }
                    };
                }
                catch (Exception ex) when (TrySetExceptionReturnFalse(startCompletionSource, ex))
                {
                    throw;
                }
            }

            private static bool TrySetExceptionReturnFalse(TaskCompletionSource<object> source, Exception ex)
            {
                source.TrySetException(ex);
                return false;
            }
        }
    }
}
