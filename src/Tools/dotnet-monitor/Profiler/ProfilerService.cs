// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class ProfilerService
    {
        private static class RuntimeIdentifierSource
        {
            // Retrieved from the environment block of the target process
            public const string ProcessEnvironment = nameof(ProcessEnvironment);

            // Retrieved from the host of the target process
            public const string ProcessHost = nameof(ProcessHost);

            // Implicitly determined from some heuristic based on what is running in the target process
            public const string ProcessImplicit = nameof(ProcessImplicit);
        }

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

                string runtimeIdentifierSource = RuntimeIdentifierSource.ProcessEnvironment;
                if (!env.TryGetValue(ToolIdentifiers.EnvironmentVariables.RuntimeIdentifier, out string runtimeIdentifier))
                {
                    ProcessInfo processInfo = await client.GetProcessInfoAsync(cancellationToken);

                    // Use portable runtime identifier as reported by the runtime
                    runtimeIdentifier = processInfo.PortableRuntimeIdentifier;
                    runtimeIdentifierSource = RuntimeIdentifierSource.ProcessHost;

                    if (string.IsNullOrEmpty(runtimeIdentifier))
                    {
                        runtimeIdentifierSource = RuntimeIdentifierSource.ProcessImplicit;
                        // This is mostly correct, except that "arm" and "armv6" are both reported as "arm32".
                        string ridArchitecture = processInfo.ProcessArchitecture;
                        Debug.Assert(!"arm32".Equals(ridArchitecture, StringComparison.Ordinal), "Unable to distinguish arm from armv6");

                        if (!string.IsNullOrEmpty(ridArchitecture) && !ridArchitecture.Equals("Unknown", StringComparison.Ordinal))
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                runtimeIdentifier = FormattableString.Invariant($"win-{ridArchitecture}");
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            {
                                runtimeIdentifier = FormattableString.Invariant($"osx-{ridArchitecture}");
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                // Make best effort to determine which type of C library is used
                                // in order to pick the correct Linux RID.
                                try
                                {
                                    Process process = Process.GetProcessById((int)processInfo.ProcessId);

                                    for (int i = 0; i < process.Modules.Count; i++)
                                    {
                                        ProcessModule module = process.Modules[i];
                                        if (module.ModuleName.StartsWith("libc.", StringComparison.Ordinal) ||
                                            module.ModuleName.StartsWith("libc-", StringComparison.Ordinal))
                                        {
                                            runtimeIdentifier = FormattableString.Invariant($"linux-{ridArchitecture}");
                                            break;
                                        }
                                        else if (module.ModuleName.StartsWith("ld-musl-", StringComparison.Ordinal))
                                        {
                                            runtimeIdentifier = FormattableString.Invariant($"linux-musl-{ridArchitecture}");
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(runtimeIdentifier))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_UnableToDetermineTargetPlatform);
                }
                else
                {
                    _logger.ProfilerRuntimeIdentifier(runtimeIdentifier, runtimeIdentifierSource);

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

                    await client.SetEnvironmentVariableAsync(
                        ProfilerIdentifiers.EnvironmentVariables.ModulePath,
                        profilerFileInfo.PhysicalPath,
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
