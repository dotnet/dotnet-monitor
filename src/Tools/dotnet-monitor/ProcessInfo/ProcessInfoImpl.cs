// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // Named "ProcessInfoImpl" to avoid collisions with
    // Microsoft.Diagnostics.NETCore.Client.ProcessInfo data structure.
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class ProcessInfoImpl : IProcessInfo
    {
        // The amount of time to wait before cancelling get additional process information (e.g. getting
        // the process command line if the IProcessInfo doesn't provide it).
        private static readonly TimeSpan ExtendedProcessInfoTimeout = TimeSpan.FromSeconds(1);

        // String returned for a process field when its value could not be retrieved. This is the same
        // value that is returned by the runtime when it could not determine the value for each of those fields.
        private static readonly string ProcessFieldUnknownValue = "unknown";

        // The value of the operating system field of the ProcessInfo result when the target process is running
        // on a Windows operating system.
        private const string ProcessOperatingSystemWindowsValue = "windows";

        public static async Task<IProcessInfo> FromProcessIdAsync(
            int processId,
            CancellationToken token)
        {
            var client = new DiagnosticsClient(processId);

            ProcessInfo processInfo = null;
            try
            {
                // Primary motivation is to get the runtime instance cookie in order to
                // keep parity with the FromIpcEndpointInfo implementation; store the
                // remainder of the information since it already has access to it.
                processInfo = await client.GetProcessInfoAsync(token);

                Debug.Assert(processId == unchecked((int)processInfo.ProcessId));
            }
            catch (ServerErrorException)
            {
                // The runtime likely doesn't understand the GetProcessInfo command.
            }
            catch (TimeoutException)
            {
                // Runtime didn't respond within client timeout.
            }

            // CONSIDER: Generate a runtime instance identifier based on the pipe name
            // for .NET Core 3.1 e.g. pid + disambiguator in GUID form.
            ProcessInfoImpl processInfoImpl = new()
            {
                Endpoint = new PidIpcEndpoint(processId),
                ProcessId = processId,
                RuntimeInstanceCookie = processInfo?.RuntimeInstanceCookie ?? Guid.Empty,
                CommandLine = processInfo?.CommandLine,
                OperatingSystem = processInfo?.OperatingSystem,
                ProcessArchitecture = processInfo?.ProcessArchitecture
            };

            await FillExtraAsync(client, processInfoImpl, token);

            return processInfoImpl;
        }

        public static async Task<IProcessInfo> FromIpcEndpointInfoAsync(
            IpcEndpointInfo info,
            CancellationToken token)
        {
            var client = new DiagnosticsClient(info.Endpoint);

            ProcessInfo processInfo = null;
            try
            {
                // Primary motivation is to keep parity with the FromProcessId implementation,
                // which provides the additional process information because it already has
                // access to it.
                processInfo = await client.GetProcessInfoAsync(token);

                Debug.Assert(info.ProcessId == unchecked((int)processInfo.ProcessId));
                Debug.Assert(info.RuntimeInstanceCookie == processInfo.RuntimeInstanceCookie);
            }
            catch (ServerErrorException)
            {
                // The runtime likely doesn't understand the GetProcessInfo command.
            }
            catch (TimeoutException)
            {
                // Runtime didn't respond within client timeout.
            }

            ProcessInfoImpl processInfoImpl = new()
            {
                Endpoint = info.Endpoint,
                ProcessId = info.ProcessId,
                RuntimeInstanceCookie = info.RuntimeInstanceCookie,
                CommandLine = processInfo?.CommandLine,
                OperatingSystem = processInfo?.OperatingSystem,
                ProcessArchitecture = processInfo?.ProcessArchitecture
            };

            await FillExtraAsync(client, processInfoImpl, token);

            return processInfoImpl;
        }

        private static async Task FillExtraAsync(
            DiagnosticsClient client,
            ProcessInfoImpl processInfoImpl,
            CancellationToken token)
        {
            string commandLine = processInfoImpl.CommandLine;
            if (string.IsNullOrEmpty(commandLine))
            {
                EventProcessInfoPipelineSettings infoSettings = new()
                {
                    Duration = ExtendedProcessInfoTimeout,
                };

                await using EventProcessInfoPipeline pipeline = new(
                    client,
                    infoSettings,
                    (cmdLine, token) => { commandLine = cmdLine; return Task.CompletedTask; });

                await pipeline.RunAsync(token);
            }

            string processName = null;
            if (!string.IsNullOrEmpty(commandLine))
            {
                // Get the process name from the command line
                bool isWindowsProcess = false;
                if (string.IsNullOrEmpty(processInfoImpl.OperatingSystem))
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
                    isWindowsProcess = ProcessOperatingSystemWindowsValue.Equals(processInfoImpl.OperatingSystem, StringComparison.OrdinalIgnoreCase);
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

            processInfoImpl.CommandLine = commandLine ?? ProcessFieldUnknownValue;
            processInfoImpl.ProcessName = processName ?? ProcessFieldUnknownValue;
        }

        public IpcEndpoint Endpoint { get; private set; }

        public int ProcessId { get; private set; }

        public Guid RuntimeInstanceCookie { get; private set; }

        public string CommandLine { get; private set; }

        public string OperatingSystem { get; private set; }

        public string ProcessArchitecture { get; private set; }

        public string ProcessName { get; private set; }

        internal string DebuggerDisplay => FormattableString.Invariant($"PID={ProcessId}, Cookie={RuntimeInstanceCookie}");
    }
}
