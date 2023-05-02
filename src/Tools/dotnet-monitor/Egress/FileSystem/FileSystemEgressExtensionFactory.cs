// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    internal sealed class FileSystemEgressExtensionFactory : IWellKnownExtensionFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly ILogger<FileSystemEgressExtension> _logger;

        public FileSystemEgressExtensionFactory(
            IServiceProvider serviceProvider,
            IEgressConfigurationProvider configurationProvider,
            ILogger<FileSystemEgressExtension> logger)
        {
            _serviceProvider = serviceProvider;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        IEgressExtension IWellKnownExtensionFactory.Create()
        {
            return new FileSystemEgressExtension(_serviceProvider, _configurationProvider, _logger);
        }


        public string Name => EgressProviderTypes.FileSystem;
    }
}
