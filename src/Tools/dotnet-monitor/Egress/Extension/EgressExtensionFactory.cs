// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Extension
{
    internal sealed class EgressExtensionFactory
    {
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly ILogger<EgressExtension> _logger;
        private readonly IOptions<ValidationOptions> _validationOptions;

        public EgressExtensionFactory(
            IEgressConfigurationProvider configurationProvider,
            ILogger<EgressExtension> logger,
            IOptions<ValidationOptions> validationOptions)
        {
            _configurationProvider = configurationProvider;
            _logger = logger;
            _validationOptions = validationOptions ?? throw new ArgumentNullException(nameof(validationOptions));
        }

        public IEgressExtension Create(ExtensionManifest manifest, string path)
        {
            return new EgressExtension(manifest, path, _configurationProvider, _logger, _validationOptions);
        }
    }
}
