﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class ProfilerService
    {
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly ISharedLibraryService _sharedLibraryService;
        private readonly IOptions<StorageOptions> _storageOptions;
        private readonly ILogger<ProfilerService> _logger;

        public ProfilerService(
            ISharedLibraryService sharedLibraryService,
            IOptions<StorageOptions> storageOptions,
            IInProcessFeatures inProcessFeatures,
            ILogger<ProfilerService> logger)
        {
            _inProcessFeatures = inProcessFeatures;
            _logger = logger;
            _sharedLibraryService = sharedLibraryService;
            _storageOptions = storageOptions;
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
                    IFileProviderFactory fileProviderFactory = await _sharedLibraryService.GetFactoryAsync(cancellationToken);

                    IFileProvider nativeFileProvider = fileProviderFactory.CreateNative(runtimeIdentifier);

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

                    // There's no way to ask the target process if it is suspended at startup.
                    // Just attempt to attach the profiler; if the process is already running,
                    // this will succeed. If failed (hopefully due to suspension) fallback to
                    // setting the startup profiler.
                    // Can't check that assumption because ServerErrorException and related
                    // exceptions do not report what the issue is beyond the human readable
                    // Exception.Message property value.
                    // Calling SetStartupProfiler on a process that is using nosuspend will
                    // (unexpectedly) succeed, thus cannot call SetStartupProfiler first and
                    // then fallback to AttachProfiler.
                    try
                    {
                        // This will wait until the profiler's ICorProfilerCallback3::InitializeForAttach
                        // implementation completes. This will throw if the target process is not running
                        // managed code yet.
                        await client.AttachProfilerAsync(
                            TimeSpan.FromSeconds(10),
                            ProfilerIdentifiers.Clsid.Guid,
                            profilerFileInfo.PhysicalPath,
                            Array.Empty<byte>(),
                            cancellationToken);
                    }
                    catch (ServerErrorException)
                    {
                        await client.SetStartupProfilerAsync(
                            ProfilerIdentifiers.Clsid.Guid,
                            profilerFileInfo.PhysicalPath,
                            cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.UnableToApplyProfiler(ex);
            }
        }
    }
}