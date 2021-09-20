// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// Named "ProcessInfoImpl" to disambiguate from Microsoft.Diagnostics.NETCore.client ProcessInfo
    /// class returned from issuing GetProcesssInfo command on diagnostic pipe.
    internal sealed class ProcessInfoImpl : IProcessInfo
    {
        // The amount of time to wait before cancelling get additional process information (e.g. getting
        // the process command line if the IEndpointInfo doesn't provide it).
        public static readonly TimeSpan ExtendedProcessInfoTimeout = TimeSpan.FromMilliseconds(1000);

        // String returned for a process field when its value could not be retrieved. This is the same
        // value that is returned by the runtime when it could not determine the value for each of those fields.
        private static readonly string ProcessFieldUnknownValue = "unknown";

        // The value of the operating system field of the ProcessInfo result when the target process is running
        // on a Windows operating system.
        private const string ProcessOperatingSystemWindowsValue = "windows";

        public ProcessInfoImpl(
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

        public static async Task<IProcessInfo> FromEndpointInfoAsync(IEndpointInfo endpointInfo)
        {
            using CancellationTokenSource extendedInfoCancellation = new(ExtendedProcessInfoTimeout);
            return await FromEndpointInfoAsync(endpointInfo, extendedInfoCancellation.Token);
        }

        // Creates a ProcessInfo object from the IEndpointInfo. Attempts to get the command line using event pipe
        // if the endpoint information doesn't provide it. The cancelation token can be used to timebox this fallback
        // mechansim.
        public static async Task<IProcessInfo> FromEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken extendedInfoCancellationToken)
        {
            if (null == endpointInfo)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            DiagnosticsClient client = new(endpointInfo.Endpoint);

            string commandLine = endpointInfo.CommandLine;
            if (string.IsNullOrEmpty(commandLine))
            {
                try
                {
                    EventProcessInfoPipelineSettings infoSettings = new()
                    {
                        Duration = Timeout.InfiniteTimeSpan,
                    };

                    await using EventProcessInfoPipeline pipeline = new(client, infoSettings,
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

            return new ProcessInfoImpl(
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
