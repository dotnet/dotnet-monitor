﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        public async Task<bool> ApplyStartupHook(IEndpointInfo endpointInfo, CancellationToken token)
        {
            if (endpointInfo.RuntimeVersion.Major >= 8)
            {
                try
                {
                    IFileInfo startupHookLibraryFileInfo = await GetStartupHookLibraryFileInfo(token);
                    DiagnosticsClient client = new(endpointInfo.Endpoint);

                    await client.ApplyStartupHookAsync(startupHookLibraryFileInfo.PhysicalPath, token);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.StartupHookApplyFailed(ex);
                }
            }

            return false;
        }

        public async Task<bool> CheckEnvironmentAsync(IEndpointInfo endpointInfo, CancellationToken token, bool logInstructions = false)
        {
            IFileInfo startupHookLibraryFileInfo = await GetStartupHookLibraryFileInfo(token);
            DiagnosticsClient client = new(endpointInfo.Endpoint);

            IDictionary<string, string> env = await client.GetProcessEnvironmentAsync(token);

            if (!env.TryGetValue(ToolIdentifiers.EnvironmentVariables.StartupHooks, out string startupHookPaths))
            {
                if (logInstructions)
                {
                    _logger.StartupHookEnvironmentMissing(endpointInfo.ProcessId);
                    LogInstructions(startupHookLibraryFileInfo);
                }

                return false;
            }

            if (string.IsNullOrEmpty(startupHookPaths) || !startupHookPaths.Contains(StartupHookFileName, StringComparison.OrdinalIgnoreCase))
            {
                if (logInstructions)
                {
                    _logger.StartupHookMissing(endpointInfo.ProcessId, startupHookLibraryFileInfo.Name);
                    LogInstructions(startupHookLibraryFileInfo);
                }

                return false;
            }

            return true;
        }

        private async Task<IFileInfo> GetStartupHookLibraryFileInfo(CancellationToken token)
        {
            IFileProviderFactory fileProviderFactory = await _sharedLibraryService.GetFactoryAsync(token);

            IFileProvider managedFileProvider = fileProviderFactory.CreateManaged(StartupHookTargetFramework);

            return managedFileProvider.GetFileInfo(StartupHookFileName);
        }

        private void LogInstructions(IFileInfo startupHookLibraryFileInfo)
        {
            _logger.StartupHookInstructions(startupHookLibraryFileInfo.PhysicalPath);
        }
    }
}
