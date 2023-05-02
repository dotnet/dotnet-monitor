// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Extension
{
    internal sealed class EgressExtensionFactory
    {
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly ILogger<EgressExtension> _logger;

        public EgressExtensionFactory(
            IEgressConfigurationProvider configurationProvider,
            ILogger<EgressExtension> logger)
        {
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public IEgressExtension Create(ExtensionManifest manifest, string path)
        {
            return new EgressExtension(manifest, path, _configurationProvider, _logger);
        }
    }
}
