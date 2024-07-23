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
    public sealed class StartupHookApplicator
    {
        private readonly ILogger _logger;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ISharedLibraryService _sharedLibraryService;

        public StartupHookApplicator(
            ILogger<StartupHookApplicator> logger,
            IEndpointInfo endpointInfo,
            ISharedLibraryService sharedLibraryService)
        {
            _logger = logger;
            _endpointInfo = endpointInfo;
            _sharedLibraryService = sharedLibraryService;
        }

        public async Task<bool> ApplyAsync(string tfm, string fileName, CancellationToken token)
        {
            IFileInfo fileInfo = await GetFileInfoAsync(tfm, fileName, token);
            if (!fileInfo.Exists)
            {
                // When the file doesn't exist fileInfo.Name will contain the full path of the missing file.
                _logger.StartupHookApplyFailed(fileName, new FileNotFoundException(null, fileInfo.Name));
                return false;
            }

            DiagnosticsClient client = new(_endpointInfo.Endpoint);
            IDictionary<string, string> env = await client.GetProcessEnvironmentAsync(token);

            if (IsEnvironmentConfiguredForStartupHook(fileInfo, env))
            {
                return true;
            }

            if (_endpointInfo.RuntimeVersion?.Major < 8)
            {
#nullable disable
                _logger.StartupHookInstructions(_endpointInfo.ProcessId, fileInfo.Name, fileInfo.PhysicalPath);
#nullable restore
                return false;
            }

            return await ApplyUsingDiagnosticClientAsync(fileInfo, client, token);
        }

        private async Task<IFileInfo> GetFileInfoAsync(string tfm, string fileName, CancellationToken token)
        {
            IFileProviderFactory fileProviderFactory = await _sharedLibraryService.GetFactoryAsync(token);
            IFileProvider managedFileProvider = fileProviderFactory.CreateManaged(tfm);
            return managedFileProvider.GetFileInfo(fileName);
        }

        private static bool IsEnvironmentConfiguredForStartupHook(IFileInfo fileInfo, IDictionary<string, string> env)
            => env.TryGetValue(ToolIdentifiers.EnvironmentVariables.StartupHooks, out string? startupHookPaths) &&
            startupHookPaths?.Contains(fileInfo.Name, StringComparison.OrdinalIgnoreCase) == true;

        private async Task<bool> ApplyUsingDiagnosticClientAsync(IFileInfo fileInfo, DiagnosticsClient client, CancellationToken token)
        {
            try
            {
                await client.ApplyStartupHookAsync(fileInfo.PhysicalPath, token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.StartupHookApplyFailed(fileInfo.Name, ex);
                return false;
            }
        }
    }
}
