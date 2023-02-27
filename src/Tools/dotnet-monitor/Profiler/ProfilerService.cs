// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly ISharedLibraryInitializer _sharedLibraryInitializer;
        private readonly IOptions<StorageOptions> _storageOptions;
        private readonly ILogger<ProfilerService> _logger;

        public ProfilerService(
            ISharedLibraryInitializer sharedLibraryInitializer,
            IOptions<StorageOptions> storageOptions,
            IInProcessFeatures inProcessFeatures,
            ILogger<ProfilerService> logger)
        {
            _inProcessFeatures = inProcessFeatures;
            _logger = logger;
            _sharedLibraryInitializer = sharedLibraryInitializer;
            _storageOptions = storageOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_inProcessFeatures.IsProfilerRequired)
            {
                return;
            }

            using IDisposable _ = stoppingToken.Register(() => _fileProviderFactorySource.TrySetCanceled(stoppingToken));

            // Yield to allow other hosting services to start
            await Task.Yield();

            try
            {
                _fileProviderFactorySource.SetResult(_sharedLibraryInitializer.Initialize());
            }
            catch (Exception ex)
            {
                _logger.FailedInitializeSharedLibraryStorage(ex);

                _fileProviderFactorySource.SetException(ex);
            }
        }

        public async Task ApplyProfiler(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            if (!_inProcessFeatures.IsProfilerRequired)
            {
                return;
            }

            // The profiler is only supported on .NET 6+
            if (null == endpointInfo.RuntimeVersion || endpointInfo.RuntimeVersion.Major < 6)
            {
                return;
            }

            try
            {
                DiagnosticsClient client = new DiagnosticsClient(endpointInfo.Endpoint);
                Dictionary<string, string> env = await client.GetProcessEnvironmentAsync(cancellationToken);

                // CONSIDER: Allow alternate means of specifying or heuristically determining the runtime identifier
                // - For Windows and OSX, build identifier from platform + architecture of target process.
                // - Lookup same environment variable on dotnet-monitor itself.
                // - Check libc type of dotnet-monitor.
                env.TryGetValue(ToolIdentifiers.EnvironmentVariables.RuntimeIdentifier, out string runtimeIdentifier);

                if (string.IsNullOrEmpty(runtimeIdentifier))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_UnableToDetermineTargetPlatform);
                }
                else
                {
                    INativeFileProviderFactory fileProviderFactory = await _fileProviderFactorySource.Task;

                    IFileProvider nativeFileProvider = fileProviderFactory.Create(runtimeIdentifier);

                    string libraryName = NativeLibraryHelper.GetSharedLibraryName(ProfilerIdentifiers.LibraryRootFileName);

                    IFileInfo profilerFileInfo = nativeFileProvider.GetFileInfo(libraryName);
                    if (!profilerFileInfo.Exists)
                    {
                        // Do not use IFileInfo.PhysicalPath as that is null of files that do not exist.
                        throw new FileNotFoundException(Strings.ErrorMessage_UnableToFindProfilerAssembly, profilerFileInfo.Name);
                    }

                    // This optional setting instructs where the profiler should establish its socket file
                    // and where to provide any additional files to dotnet-monitor.
                    // CONSIDER: Include the runtime instance identifier in the path in order to keep
                    // target processes assets separated from one another.
                    string defaultSharedPath = _storageOptions.Value.DefaultSharedPath;
                    if (!string.IsNullOrEmpty(defaultSharedPath))
                    {
                        // Create sharing directory in case it doesn't exist.
                        Directory.CreateDirectory(defaultSharedPath);

                        await client.SetEnvironmentVariableAsync(
                            ProfilerIdentifiers.EnvironmentVariables.SharedPath,
                            defaultSharedPath,
                            cancellationToken);
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
                _logger.UnableToApplyProfiler(ex);
            }
        }
    }
}
