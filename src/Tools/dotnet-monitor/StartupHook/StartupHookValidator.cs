// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookValidator
    {
        // Intent is to ship a single TFM of the startup hook, which should be the lowest supported version.
        // If necessary, the startup hook should dynamically access APIs for higher version TFMs and handle
        // all exceptions appropriately.
        private const string StartupHookFileName = "Microsoft.Diagnostics.Monitoring.StartupHook.dll";
        private const string StartupHookTargetFramework = "net6.0";


        private readonly ILogger _logger;
        private readonly ISharedLibraryService _sharedLibraryService;

        public StartupHookValidator(
            ISharedLibraryService sharedLibraryService,
            ILogger<StartupHookValidator> logger)
        {
            _logger = logger;
            _sharedLibraryService = sharedLibraryService;
        }

        public async Task<bool> CheckAsync(IEndpointInfo endpointInfo, CancellationToken token)
        {
            IFileProviderFactory fileProviderFactory = await _sharedLibraryService.GetFactoryAsync(token);

            IFileProvider managedFileProvider = fileProviderFactory.CreateManaged(StartupHookTargetFramework);

            IFileInfo startupHookLibraryFileInfo = managedFileProvider.GetFileInfo(StartupHookFileName);

            if (!startupHookLibraryFileInfo.Exists)
            {
                // This would be a bug in dotnet-monitor; throw appropriate non-MonitoringException instance.
                throw new FileNotFoundException(null, startupHookLibraryFileInfo.Name);
            }

            DiagnosticsClient client = new(endpointInfo.Endpoint);
            IDictionary<string, string> env = await client.GetProcessEnvironmentAsync(token);

            if (!env.TryGetValue(ToolIdentifiers.EnvironmentVariables.StartupHooks, out string startupHookPaths))
            {
                _logger.StartupHookEnvironmentMissing(endpointInfo.ProcessId);

                LogInstructions(startupHookLibraryFileInfo);

                return false;
            }

            if (string.IsNullOrEmpty(startupHookPaths) || !startupHookPaths.Contains(StartupHookFileName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.StartupHookMissing(endpointInfo.ProcessId, startupHookLibraryFileInfo.Name);

                LogInstructions(startupHookLibraryFileInfo);

                return false;
            }

            return true;
        }

        private void LogInstructions(IFileInfo startupHookLibraryFileInfo)
        {
            _logger.StartupHookInstructions(startupHookLibraryFileInfo.PhysicalPath);
        }
    }
}
