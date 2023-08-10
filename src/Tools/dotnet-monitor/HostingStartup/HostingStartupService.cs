// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.HostingStartup
{
    internal sealed class HostingStartupService
        : IDiagnosticLifetimeService
    {
        // Intent is to ship a single TFM of the hosting startup, which should be the lowest supported version.
        private const string HostingStartupFileName = "Microsoft.Diagnostics.Monitoring.HostingStartup.dll";
        private const string HostingStartupTargetFramework = "net6.0";

        private readonly IEndpointInfo _endpointInfo;
        private readonly StartupHookService _startupHookService;
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly ISharedLibraryService _sharedLibraryService;
        private readonly ILogger<HostingStartupService> _logger;

        public HostingStartupService(
            IEndpointInfo endpointInfo,
            StartupHookService startupHookService,
            ISharedLibraryService sharedLibraryService,
            IInProcessFeatures inProcessFeatures,
            ILogger<HostingStartupService> logger)
        {
            _endpointInfo = endpointInfo;
            _startupHookService = startupHookService;
            _inProcessFeatures = inProcessFeatures;
            _sharedLibraryService = sharedLibraryService;
            _logger = logger;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (!_inProcessFeatures.IsHostingStartupRequired)
            {
                return;
            }

            // Hosting startup is only supported on .NET 6+
            if (_endpointInfo.RuntimeVersion == null || _endpointInfo.RuntimeVersion.Major < 6)
            {
                return;
            }

            try
            {
                // Hosting startup requires the startup hook
                if (!await _startupHookService.CheckHasStartupHookAsync(cancellationToken))
                {
                    return;
                }

                IFileProviderFactory fileProviderFactory = await _sharedLibraryService.GetFactoryAsync(cancellationToken);
                IFileProvider managedFileProvider = fileProviderFactory.CreateManaged(HostingStartupTargetFramework);

                IFileInfo hostingStartupLibraryFileInfo = managedFileProvider.GetFileInfo(HostingStartupFileName);
                if (!hostingStartupLibraryFileInfo.Exists)
                {
                    throw new FileNotFoundException(Strings.ErrorMessage_UnableToFindHostingStartupAssembly, hostingStartupLibraryFileInfo.Name);
                }

                DiagnosticsClient client = new DiagnosticsClient(_endpointInfo.Endpoint);
                await client.SetEnvironmentVariableAsync(
                    StartupHookIdentifiers.EnvironmentVariables.HostingStartupPath,
                    hostingStartupLibraryFileInfo.PhysicalPath,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.UnableToApplyHostingStartup(ex);
            }
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
