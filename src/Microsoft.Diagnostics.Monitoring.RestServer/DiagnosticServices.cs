// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    internal sealed class DiagnosticServices : IDiagnosticServices
    {
        // The value of the operating system field of the ProcessInfo result when the target process is running
        // on a Windows operating system.
        private const string ProcessOperatingSystemWindowsValue = "windows";

        // The amount of time to wait before cancelling get additional process information (e.g. getting
        // the process command line if the IEndpointInfo doesn't provide it).
        private static readonly TimeSpan ExtendedProcessInfoTimeout = TimeSpan.FromMilliseconds(500);

        private readonly IEndpointInfoSourceInternal _endpointInfoSource;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly IOptionsMonitor<StorageOptions> _storageOptions;
        private readonly IOptionsMonitor<ProcessFilterOptions> _defaultProcessOptions;

        public DiagnosticServices(IEndpointInfoSource endpointInfoSource,
            IOptionsMonitor<StorageOptions> storageOptions,
            IOptionsMonitor<ProcessFilterOptions> defaultProcessMonitor)
        {
            _endpointInfoSource = (IEndpointInfoSourceInternal)endpointInfoSource;
            _storageOptions = storageOptions;
            _defaultProcessOptions = defaultProcessMonitor;
        }

        public async Task<IEnumerable<IProcessInfo>> GetProcessesAsync(DiagProcessFilter processFilterConfig, CancellationToken token)
        {
            IEnumerable<IProcessInfo> processes = null;

            try
            {
                using CancellationTokenSource extendedInfoCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                IList<Task<ProcessInfo>> processInfoTasks = new List<Task<ProcessInfo>>();
                foreach (IEndpointInfo endpointInfo in await _endpointInfoSource.GetEndpointInfoAsync(token))
                {
                    // CONSIDER: Can this processing be pushed into the IEndpointInfoSource implementation and cached
                    // so that extended process information doesn't have to be recalculated for every call. This would be
                    // useful for:
                    // - .NET Core 3.1 processes, which require issuing a brief event pipe session to get the process commmand
                    //   line information and parse out the process name
                    // - Caching entrypoint information (when that becomes available).
                    processInfoTasks.Add(ProcessInfo.FromEndpointInfoAsync(endpointInfo, extendedInfoCancellation.Token));
                }

                // FromEndpointInfoAsync can fill in the command line for .NET Core 3.1 processes by invoking the
                // event pipe and capturing the ProcessInfo event. Timebox this operation with the cancellation token
                // so that getting the process list does not take a long time or wait indefinitely.
                extendedInfoCancellation.CancelAfter(ExtendedProcessInfoTimeout);

                await Task.WhenAll(processInfoTasks);

                processes = processInfoTasks.Select(t => t.Result);
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Unable to enumerate processes.");
            }

            if (processFilterConfig != null)
            {
                processes = processes.Where(p => processFilterConfig.Filters.All(c => c.MatchFilter(p)));
            }

            return processes;
        }
        public async Task<Stream> GetDump(IProcessInfo pi, Models.DumpType mode, CancellationToken token)
        {
            string dumpFilePath = Path.Combine(_storageOptions.CurrentValue.DumpTempFolder, FormattableString.Invariant($"{Guid.NewGuid()}_{pi.EndpointInfo.ProcessId}"));
            NETCore.Client.DumpType dumpType = MapDumpType(mode);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Get the process
                Process process = Process.GetProcessById(pi.EndpointInfo.ProcessId);
                await Dumper.CollectDumpAsync(process, dumpFilePath, dumpType);
            }
            else
            {
                await Task.Run(() =>
                {
                    var client = new DiagnosticsClient(pi.EndpointInfo.Endpoint);
                    client.WriteDump(dumpType, dumpFilePath);
                });
            }

            return new AutoDeleteFileStream(dumpFilePath);
        }

        private static DumpType MapDumpType(Models.DumpType dumpType)
        {
            switch (dumpType)
            {
                case Models.DumpType.Full:
                    return DumpType.Full;
                case Models.DumpType.WithHeap:
                    return DumpType.WithHeap;
                case Models.DumpType.Triage:
                    return DumpType.Triage;
                case Models.DumpType.Mini:
                    return DumpType.Normal;
                default:
                    throw new ArgumentException("Unexpected dumpType", nameof(dumpType));
            }
        }

        public Task<IProcessInfo> GetProcessAsync(ProcessKey? processKey, CancellationToken token)
        {
            DiagProcessFilter filterOptions = null;
            if (processKey.HasValue)
            {
                filterOptions = DiagProcessFilter.FromProcessKey(processKey.Value);
            }
            else
            {
                filterOptions = DiagProcessFilter.FromConfiguration(_defaultProcessOptions.CurrentValue);
            }

            return GetProcessAsync(filterOptions, token);
        }

        private async Task<IProcessInfo> GetProcessAsync(DiagProcessFilter processFilterConfig, CancellationToken token)
        {
            //Short circuit when we are missing default process config
            if (!processFilterConfig.Filters.Any())
            {
                throw new InvalidOperationException("No default process configuration has been set.");
            }
            IEnumerable<IProcessInfo> matchingProcesses = await GetProcessesAsync(processFilterConfig, token);

            switch (matchingProcesses.Count())
            {
                case 0:
                    throw new ArgumentException("Unable to discover a target process.");
                case 1:
                    return matchingProcesses.First();
                default:
                    throw new ArgumentException("Unable to select a single target process because multiple target processes have been discovered.");
            }
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
        }

        /// <summary>
        /// We want to make sure we destroy files we finish streaming.
        /// We want to make sure that we stream out files since we compress on the fly; the size cannot be known upfront.
        /// CONSIDER The above implies knowledge of how the file is used by the rest api.
        /// </summary>
        private sealed class AutoDeleteFileStream : FileStream
        {
            public AutoDeleteFileStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096, FileOptions.DeleteOnClose)
            {
            }

            public override bool CanSeek => false;
        }


        private sealed class ProcessInfo : IProcessInfo
        {
            // String returned for a process field when its value could not be retrieved. This is the same
            // value that is returned by the runtime when it could not determine the value for each of those fields.
            private const string ProcessFieldUnknownValue = "unknown";

            public ProcessInfo(
                IEndpointInfo endpointInfo,
                string commandLine,
                string processName)
            {
                EndpointInfo = endpointInfo;

                // The GetProcessInfo command will return "unknown" for values for which it does
                // not know the value, such as operating system and process architecture if the
                // process is running on one that is not predefined. Mimic the same behavior here
                // when the extra process information was not provided.
                CommandLine = commandLine ?? ProcessFieldUnknownValue;
                ProcessName = processName ?? ProcessFieldUnknownValue;
            }

            public static async Task<ProcessInfo> FromEndpointInfoAsync(IEndpointInfo endpointInfo)
            {
                using CancellationTokenSource extendedInfoCancellation = new CancellationTokenSource(ExtendedProcessInfoTimeout);
                return await FromEndpointInfoAsync(endpointInfo, extendedInfoCancellation.Token);
            }

            // Creates a ProcessInfo object from the IEndpointInfo. Attempts to get the command line using event pipe
            // if the endpoint information doesn't provide it. The cancelation token can be used to timebox this fallback
            // mechansim.
            public static async Task<ProcessInfo> FromEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken extendedInfoCancellationToken)
            {
                if (null == endpointInfo)
                {
                    throw new ArgumentNullException(nameof(endpointInfo));
                }

                var client = new DiagnosticsClient(endpointInfo.Endpoint);

                string commandLine = endpointInfo.CommandLine;
                if (string.IsNullOrEmpty(commandLine))
                {
                    try
                    {
                        var infoSettings = new EventProcessInfoPipelineSettings
                        {
                            Duration = Timeout.InfiniteTimeSpan,
                        };

                        await using var pipeline = new EventProcessInfoPipeline(client, infoSettings,
                            (cmdLine, token) => { commandLine = cmdLine; return Task.CompletedTask; });

                        await pipeline.RunAsync(extendedInfoCancellationToken);
                    }
                    catch
                    {
                    }
                }

                string processName = null;
                if (!string.IsNullOrEmpty(commandLine))
                {
                    // Get the process name from the command line
                    bool isWindowsProcess = false;
                    if (string.IsNullOrEmpty(endpointInfo.OperatingSystem))
                    {
                        // If operating system is null, the process is likely .NET Core 3.1 (which doesn't have the GetProcessInfo command).
                        // Since the underlying diagnostic communication channel used by the .NET runtime requires that the diagnostic process
                        // must be running on the same type of operating system as the target process (e.g. dotnet-monitor must be running on Windows
                        // if the target process is running on Windows), then checking the local operating system should be a sufficient heuristic
                        // to determine the operating system of the target process.
                        isWindowsProcess = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    }
                    else
                    {
                        isWindowsProcess = ProcessOperatingSystemWindowsValue.Equals(endpointInfo.OperatingSystem, StringComparison.OrdinalIgnoreCase);
                    }

                    string processPath = CommandLineHelper.ExtractExecutablePath(commandLine, isWindowsProcess);
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        processName = Path.GetFileName(processPath);
                        if (isWindowsProcess)
                        {
                            // Remove the extension on Windows to match the behavior of Process.ProcessName
                            processName = Path.GetFileNameWithoutExtension(processName);
                        }
                    }
                }

                return new ProcessInfo(
                    endpointInfo,
                    commandLine,
                    processName);
            }

            public IEndpointInfo EndpointInfo { get; }

            public string CommandLine { get; }

            public string OperatingSystem => EndpointInfo.OperatingSystem ?? ProcessFieldUnknownValue;

            public string ProcessArchitecture => EndpointInfo.ProcessArchitecture ?? ProcessFieldUnknownValue;

            public string ProcessName { get; }
        }
    }
}
