// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using System.IO.Pipelines;

namespace CollectionRuleActions.UnitTests
{
    internal sealed class PipeEgressExtensionFactory : IWellKnownExtensionFactory
    {
        private readonly PipeWriter _writer;

        public PipeEgressExtensionFactory(PipeWriter writer)
        {
            _writer = writer;
        }

        public IEgressExtension Create()
        {
            return new PipeEgressExtension(_writer);
        }

        public string Name => PipeEgressExtension.Name;
    }
}
