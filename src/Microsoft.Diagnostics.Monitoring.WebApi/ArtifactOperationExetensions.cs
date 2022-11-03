// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class ArtifactOperationExetensions
    {
        public static async Task ExecuteAsync(this IArtifactOperation operation, Stream outputStream, CancellationToken token)
        {
            await operation.StartAsync(outputStream, token);

            await operation.WaitForCompletionAsync(token);
        }

        public static async Task ExecuteAsync(this IArtifactOperation operation, Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            await operation.StartAsync(outputStream, token);

            startCompletionSource.TrySetResult(null);

            await operation.WaitForCompletionAsync(token);
        }
    }
}
