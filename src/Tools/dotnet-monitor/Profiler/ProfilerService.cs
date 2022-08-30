// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class ProfilerService : BackgroundService
    {
        private static readonly string SharedLibrarySourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shared");

        private readonly TaskCompletionSource<string> _libraryPathSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ILogger<ProfilerService> _logger;
        private readonly IOptions<StorageOptions> _storageOptions;

        public ProfilerService(
            IOptions<StorageOptions> storageOptions,
            ILogger<ProfilerService> logger)
        {
            _logger = logger;
            _storageOptions = storageOptions;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IDisposable _ = stoppingToken.Register(() => _libraryPathSource.TrySetCanceled(stoppingToken));

            // Yield to allow other hosting services to start
            await Task.Yield();

            // Copy the shared libraries to the path specified by Storage:SharedLibraryPath.
            // Copying, instead of linking or using them in-place, prevents file locks from the target process.
            // If shared path is not specified, use the libraries in-place.
            string sharedLibraryTargetPath = _storageOptions.Value.SharedLibraryPath;
            DirectoryInfo sharedLibrarySourceDir = new DirectoryInfo(SharedLibrarySourcePath);
            if (sharedLibrarySourceDir.Exists)
            {
                if (string.IsNullOrEmpty(sharedLibraryTargetPath))
                {
                    _libraryPathSource.SetResult(SharedLibrarySourcePath);
                }
                else
                {
                    try
                    {
                        // CONSIDER: Rather than using a version file, copy the shared assemblies into a subpath that
                        // contains the dotnet-monitor version number. This would prevent contention in scenarios that
                        // use a rolling upgrade strategy but do not clear out the temporary mounted filesystems. However,
                        // this would drastically increase the success bar for scenarios that require manual specification
                        // of the absolute location of files within the shared path e.g. using DOTNET_STARTUP_HOOKS.
                        FileInfo versionFileInfo = new FileInfo(Path.Combine(sharedLibraryTargetPath, "version.txt"));
                        string expectedVersion = Assembly.GetExecutingAssembly().GetInformationalVersionString();

                        if (Directory.Exists(sharedLibraryTargetPath))
                        {
                            if (!versionFileInfo.Exists)
                            {
                                throw new FileNotFoundException("Expected version file to exist.", versionFileInfo.FullName);
                            }

                            using StreamReader reader = versionFileInfo.OpenText();
                            string version = await reader.ReadToEndAsync();
                            if (!string.Equals(expectedVersion, version))
                            {
                                throw new InvalidOperationException("Assemblies at shared storage location are incompatible.");
                            }
                        }
                        else
                        {
                            sharedLibrarySourceDir.CopyContentsTo(Directory.CreateDirectory(sharedLibraryTargetPath));

                            using StreamWriter writer = versionFileInfo.CreateText();
                            await writer.WriteAsync(expectedVersion.AsMemory(), stoppingToken);
                            await writer.FlushAsync();
                        }

                        _libraryPathSource.SetResult(sharedLibraryTargetPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize shared storage.");

                        _libraryPathSource.SetException(ex);
                    }
                }
            }
            else
            {
                // This condition occurs when runing dotnet-monitor from the build output because
                // the shared libraries are colocated into the "shared" folder due to packaging, not
                // at build time. This setting is valid for testing scenarios.
                // CONSIDER: Remove test-specific code and conditions, if possible, and replace with
                // some other extensible mechanism.
                _libraryPathSource.SetResult(null);
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
                    throw new InvalidOperationException("Unable to determine platform of the target platform.");
                }
                else
                {
                    // This may be null if running dotnet-monitor from build output instead of from
                    // dotnet tool installation.
                    string sharedLibraryPath = await _libraryPathSource.Task;

                    IFileProvider nativeFileProvider;
                    if (string.IsNullOrEmpty(sharedLibraryPath))
                    {
                        // CONSIDER: Allow some way to of specifying the build output location of the libraries
                        // without have to code it into the product.
                        nativeFileProvider = NativeFileProvider.CreateTest(runtimeIdentifier);
                    }
                    else
                    {
                        nativeFileProvider = NativeFileProvider.CreateShared(runtimeIdentifier, sharedLibraryPath);
                    }

                    string libraryName = NativeLibraryHelper.GetSharedLibraryName("MonitorProfiler");

                    IFileInfo profilerFileInfo = nativeFileProvider.GetFileInfo(libraryName);
                    if (!profilerFileInfo.Exists)
                    {
                        // Do not use IFileInfo.PhysicalPath as that is null of files that do not exist.
                        throw new FileNotFoundException("Could not find profiler assembly at determined path.", profilerFileInfo.Name);
                    }

                    await client.SetStartupProfilerAsync(
                        ProfilerIdentifiers.Clsid.Guid,
                        profilerFileInfo.PhysicalPath,
                        cancellationToken);

                    await client.SetEnvironmentVariableAsync(
                        ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId,
                        endpointInfo.RuntimeInstanceCookie.ToString("D"),
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
