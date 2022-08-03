// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class EnumerableExtensions
    {
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
    }
}
