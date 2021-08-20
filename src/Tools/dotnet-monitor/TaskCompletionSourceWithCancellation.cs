// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    public sealed class TaskCompletionSourceWithCancellation<T> :
        TaskCompletionSource<T>
    {
        public TaskCompletionSourceWithCancellation(TaskCreationOptions creationOptions)
            : base(creationOptions)
        {
        }

        public async Task<T> WithCancellation(CancellationToken token)
        {
            using (token.Register(source => ((TaskCompletionSourceWithCancellation<T>)source).TrySetCanceled(token), this))
            {
                return await Task.ConfigureAwait(false);
            }
        }
    }
}
