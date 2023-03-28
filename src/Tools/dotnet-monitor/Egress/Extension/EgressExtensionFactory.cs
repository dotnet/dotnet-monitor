// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Extension
{
    internal sealed class EgressExtensionFactory
    {
        private readonly IEgressProviderConfigurationProvider _configurationProvider;
        private readonly ILogger<EgressExtension> _logger;
        private readonly IEgressPropertiesProvider _propertiesProvider;

        public EgressExtensionFactory(
            IEgressProviderConfigurationProvider configurationProvider,
            IEgressPropertiesProvider propertiesProvider,
            ILogger<EgressExtension> logger)
        {
            _configurationProvider = configurationProvider;
            _logger = logger;
            _propertiesProvider = propertiesProvider;
        }

        public IEgressExtension Create(ExtensionManifest manifest, string path)
        {
            return new EgressExtension(manifest, path, _configurationProvider, _propertiesProvider, _logger);
        }
    }
}
