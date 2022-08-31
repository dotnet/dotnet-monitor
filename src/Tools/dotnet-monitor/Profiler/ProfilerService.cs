// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class ProfilerService : BackgroundService
    {
        private readonly TaskCompletionSource<INativeFileProviderFactory> _fileProviderFactorySource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ISharedLibraryInitializer _sharedLibraryInitializer;
        private readonly ILogger<ProfilerService> _logger;

        public ProfilerService(
            ISharedLibraryInitializer sharedLibraryInitializer,
            ILogger<ProfilerService> logger)
        {
            _logger = logger;
            _sharedLibraryInitializer = sharedLibraryInitializer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IDisposable _ = stoppingToken.Register(() => _fileProviderFactorySource.TrySetCanceled(stoppingToken));

            // Yield to allow other hosting services to start
            await Task.Yield();

            try
            {
                _fileProviderFactorySource.SetResult(_sharedLibraryInitializer.Initialize());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize shared storage.");

                _fileProviderFactorySource.SetException(ex);
            }
        }

        public async Task ApplyProfiler(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            try
            {
                DiagnosticsClient client = new DiagnosticsClient(endpointInfo.Endpoint);
                Dictionary<string, string> env = await client.GetProcessEnvironmentAsync(cancellationToken);

                // CONSIDER: Allow alternate means of specifying or heuristically determining the runtime identifier
                // - For Windows and OSX, build identifier from platform + architecture of target process.
                // - Lookup same environment variable on dotnet-monitor itself.
                // - Check libc type of dotnet-monitor.
                env.TryGetValue(ProfilerIdentifiers.EnvironmentVariables.RuntimeIdentifier, out string runtimeIdentifier);

                if (string.IsNullOrEmpty(runtimeIdentifier))
                {
                    throw new InvalidOperationException("Unable to determine platform of the target process.");
                }
                else
                {
                    INativeFileProviderFactory fileProviderFactory = await _fileProviderFactorySource.Task;

                    IFileProvider nativeFileProvider = fileProviderFactory.Create(runtimeIdentifier);

                    string libraryName = NativeLibraryHelper.GetSharedLibraryName("MonitorProfiler");

                    IFileInfo profilerFileInfo = nativeFileProvider.GetFileInfo(libraryName);
                    if (!profilerFileInfo.Exists)
                    {
                        // Do not use IFileInfo.PhysicalPath as that is null of files that do not exist.
                        throw new FileNotFoundException("Could not find profiler assembly at determined path.", profilerFileInfo.Name);
                    }

                    await client.SetEnvironmentVariableAsync(
                        ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId,
                        endpointInfo.RuntimeInstanceCookie.ToString("D"),
                        cancellationToken);

                    await client.SetStartupProfilerAsync(
                        ProfilerIdentifiers.Clsid.Guid,
                        profilerFileInfo.PhysicalPath,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not apply profiler.");
            }
        }
    }
}
