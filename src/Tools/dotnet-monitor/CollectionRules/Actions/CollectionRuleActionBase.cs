// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal abstract class CollectionRuleActionBase<TOptions> :
        ICollectionRuleAction,
        IAsyncDisposable
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();

        private Task<CollectionRuleActionResult> _completionTask;
        private long _disposedState;

        protected IEndpointInfo EndpointInfo { get; }

        protected TOptions Options { get; }

        protected CollectionRuleActionBase(IEndpointInfo endpointInfo, TOptions options)
        {
            // TODO: Allow null endpointInfo to allow tests to pass, but this should be provided by
            // tests since it will be required by all aspects in the future. For example, the ActionListExecutor
            // (which uses null in tests) will require this when needing to get process information for
            // the actions property bag used for token replacement.
            //EndpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            EndpointInfo = endpointInfo;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            _disposalTokenSource.SafeCancel();

            await _completionTask.SafeAwait();

            _disposalTokenSource.Dispose();
        }

        public async Task StartAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            ThrowIfDisposed();

            TaskCompletionSource<object> startCompleteSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationToken disposalToken = _disposalTokenSource.Token;
            _completionTask = Task.Run(() => ExecuteAsync(startCompleteSource, collectionRuleMetadata, disposalToken), disposalToken);

            await startCompleteSource.WithCancellation(token);
        }

        public async Task StartAsync(CancellationToken token)
        {
            await StartAsync(null, token);
        }

        public async Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
        {
            ThrowIfDisposed();

            return await _completionTask.WithCancellation(token);
        }

        private async Task<CollectionRuleActionResult> ExecuteAsync(
            TaskCompletionSource<object> startCompletionSource,
            CollectionRuleMetadata collectionRuleMetadata,
            CancellationToken token)
        {
            try
            {
                return await ExecuteCoreAsync(startCompletionSource, collectionRuleMetadata, token);
            }
            catch (Exception ex) when (TrySetExceptionReturnFalse(startCompletionSource, ex, token))
            {
                throw;
            }
        }

        private static bool TrySetExceptionReturnFalse(TaskCompletionSource<object> source, Exception ex, CancellationToken token)
        {
            if (ex is OperationCanceledException)
            {
                source.TrySetCanceled(token);
            }
            else
            {
                source.TrySetException(ex);
            }
            return false;
        }

        protected void ThrowIfDisposed()
        {
            DisposableHelper.ThrowIfDisposed(ref _disposedState, this.GetType());
        }

        protected abstract Task<CollectionRuleActionResult> ExecuteCoreAsync(
            TaskCompletionSource<object> startCompletionSource,
            CollectionRuleMetadata collectionRuleMetadata,
            CancellationToken token);
    }
}
