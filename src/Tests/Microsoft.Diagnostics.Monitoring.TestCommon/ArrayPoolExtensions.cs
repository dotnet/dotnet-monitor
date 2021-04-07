// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class ArrayPoolExtensions
    {
        public static async Task<TReturn> RentAndReturnAsync<TItem, TReturn>(this ArrayPool<TItem> pool, int minLength, Func<TItem[], Task<TReturn>> func)
        {
            TItem[] buffer = pool.Rent(minLength);
            try
            {
                return await func(buffer);
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
}
