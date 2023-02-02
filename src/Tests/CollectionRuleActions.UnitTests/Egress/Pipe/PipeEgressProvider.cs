// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace CollectionRuleActions.UnitTests
{
    internal sealed class PipeEgressProvider : IEgressProvider<PipeEgressOptions>
    {
        public const string Name = "Pipe";

        public async Task<string> EgressAsync(string providerType, string providerName, PipeEgressOptions options, Func<CancellationToken, Task<Stream>> action, EgressArtifactSettings artifactSettings, CancellationToken token)
        {
            using Stream stream = await action(token);

            await stream.CopyToAsync(options.Writer, token);

            return null;
        }

        public async Task<string> EgressAsync(string providerType, string providerName, PipeEgressOptions options, Func<Stream, CancellationToken, Task> action, EgressArtifactSettings artifactSettings, CancellationToken token)
        {
            using Stream stream = options.Writer.AsStream();

            await action(stream, token);

            return null;
        }
    }
}
