// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System.IO.Pipelines;

namespace CollectionRuleActions.UnitTests
{
    internal sealed class PipeEgressConfigureNamedOptions : IConfigureNamedOptions<PipeEgressOptions>
    {
        private readonly PipeWriter _writer;

        public PipeEgressConfigureNamedOptions(PipeWriter writer)
        {
            _writer = writer;
        }

        public void Configure(string name, PipeEgressOptions options)
        {
            options.Writer = _writer;
        }

        public void Configure(PipeEgressOptions options)
        {
            Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
        }
    }
}
