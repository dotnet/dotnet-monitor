// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
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
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

#nullable disable
        private Task<CollectionRuleActionResult> _completionTask;
#nullable restore
        private long _disposedState;

        protected IEndpointInfo EndpointInfo => ProcessInfo.EndpointInfo;

        protected IProcessInfo ProcessInfo { get; }

        protected TOptions Options { get; }


        public Task Started => _startCompletionSource.Task;

        protected CollectionRuleActionBase(IProcessInfo processInfo, TOptions options)
        {
            // TODO: Allow null processInfo to allow tests to pass, but this should be provided by
            // tests since it will be required by all aspects in the future. For example, the ActionListExecutor
            // (which uses null in tests) will require this when needing to get process information for
            // the actions property bag used for token replacement.
            //ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            ProcessInfo = processInfo;
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

        public async Task StartAsync(CollectionRuleMetadata? collectionRuleMetadata, CancellationToken token)
        {
            ThrowIfDisposed();

            CancellationToken disposalToken = _disposalTokenSource.Token;
            _completionTask = Task.Run(() => ExecuteAsync(collectionRuleMetadata, disposalToken), disposalToken);

            await _startCompletionSource.WithCancellation(token);
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

        protected bool TrySetStarted()
        {
            return _startCompletionSource.TrySetResult();
        }


        private async Task<CollectionRuleActionResult> ExecuteAsync(
            CollectionRuleMetadata? collectionRuleMetadata,
            CancellationToken token)
        {
            try
            {
                return await ExecuteCoreAsync(collectionRuleMetadata, token);
            }
            catch (OperationCanceledException)
            {
                _startCompletionSource.TrySetCanceled(token);
                throw;
            }
            catch (Exception ex)
            {
                CollectionRuleActionException collectionRuleActionException = new(ex);
                _startCompletionSource.TrySetException(collectionRuleActionException);
                throw collectionRuleActionException;
            }
        }

        protected void ThrowIfDisposed()
        {
            DisposableHelper.ThrowIfDisposed(ref _disposedState, this.GetType());
        }

        protected abstract Task<CollectionRuleActionResult> ExecuteCoreAsync(
            CollectionRuleMetadata? collectionRuleMetadata,
            CancellationToken token);
    }
}
