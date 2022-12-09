// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class EnumerableExtensions
    {
        public static IAsyncDisposable CreateItemDisposer<T>(this IEnumerable<T> items) where T : IAsyncDisposable
        {
            return new DisposeItemsDisposable<T>(items);
        }

        public static async Task DisposeItemsAsync<T>(this IEnumerable<T> items) where T : IAsyncDisposable
        {
            foreach (IAsyncDisposable disposable in items)
            {
                if (null != disposable)
                {
                    await disposable.DisposeAsync();
                }
            }
        }

        private sealed class DisposeItemsDisposable<T> :
            IAsyncDisposable
            where T : IAsyncDisposable
        {
            private readonly IEnumerable<T> _items;

            private long _disposedState;

            public DisposeItemsDisposable(IEnumerable<T> items)
            {
                _items = items;
            }

            public async ValueTask DisposeAsync()
            {
                if (!DisposableHelper.CanDispose(ref _disposedState))
                {
                    return;
                }

                await _items.DisposeItemsAsync();
            }
        }
    }
}
