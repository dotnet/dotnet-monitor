// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Extension
{
    internal sealed class WellKnownEgressExtensionRepository :
        ExtensionRepository
    {
        private readonly ILogger<FileSystemEgressExtension> _logger;

        public WellKnownEgressExtensionRepository(ILogger<FileSystemEgressExtension> logger)
        {
            _logger = logger;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            switch (extensionName)
            {
                case EgressProviderTypes.FileSystem:
                    extension = new FileSystemEgressExtension(_logger);
                    return true;
            }

            extension = null;
            return false;
        }
    }
}
