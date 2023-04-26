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
    internal sealed class PipeEgressExtension : IEgressExtension
    {
        public const string Name = "Pipe";

        private readonly PipeWriter _writer;

        public PipeEgressExtension(PipeWriter writer)
        {
            _writer = writer;
        }

        public async Task<EgressArtifactResult> EgressArtifact(string providerName, EgressArtifactSettings settings, Func<Stream, CancellationToken, Task> action, CancellationToken token)
        {
            using Stream stream = _writer.AsStream();

            await action(stream, token);

            return new EgressArtifactResult() { Succeeded = true };
        }

        public Task<EgressArtifactResult> ValidateProviderAsync(string providerName, EgressArtifactSettings settings, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string DisplayName => Name;
    }
}
