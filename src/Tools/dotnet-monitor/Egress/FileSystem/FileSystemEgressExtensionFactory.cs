// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    internal sealed class FileSystemEgressExtensionFactory : IWellKnownExtensionFactory
    {
        private readonly ILogger<FileSystemEgressExtension> _logger;

        public FileSystemEgressExtensionFactory(ILogger<FileSystemEgressExtension> logger)
        {
            _logger = logger;
        }

        IEgressExtension IWellKnownExtensionFactory.Create()
        {
            return new FileSystemEgressExtension(_logger);
        }


        public string Name => EgressProviderTypes.FileSystem;
    }
}
