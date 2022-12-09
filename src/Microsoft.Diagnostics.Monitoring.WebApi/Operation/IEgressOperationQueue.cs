// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressOperationQueue
    {
        ValueTask EnqueueAsync(EgressRequest workItem);
        ValueTask<EgressRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
