﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private bool _isDisposed;

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
            lock (_disposalTokenSource)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }

            _disposalTokenSource.SafeCancel();

            await _completionTask.SafeAwait();

            _disposalTokenSource.Dispose();
        }

        public async Task StartAsync(CancellationToken token)
        {
            ThrowIfDisposed();

            TaskCompletionSource<object> startCompleteSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationToken disposalToken = _disposalTokenSource.Token;
            _completionTask = Task.Run(() => ExecuteAsync(startCompleteSource, disposalToken), disposalToken);

            await startCompleteSource.WithCancellation(token);
        }

        public async Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
        {
            ThrowIfDisposed();

            return await _completionTask.WithCancellation(token);
        }

        private async Task<CollectionRuleActionResult> ExecuteAsync(
            TaskCompletionSource<object> startCompletionSource,
            CancellationToken token)
        {
            try
            {
                return await ExecuteCoreAsync(startCompletionSource, token);
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

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        protected abstract Task<CollectionRuleActionResult> ExecuteCoreAsync(
            TaskCompletionSource<object> startCompletionSource,
            CancellationToken token);
    }
}
