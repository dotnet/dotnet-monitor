// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal interface IEgressProviderInternal
    {
        Task<string> EgressAsync(
            string providerType,
            string providerName,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token);

        Task<string> EgressAsync(
            string providerType,
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token);
    }

    internal interface IEgressProviderInternal<TOptions> :
        IEgressProviderInternal
    {
    }
}
