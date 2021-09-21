// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class EndpointInfo : IEndpointInfo
    {
        public static async Task<EndpointInfo> FromProcessIdAsync(int processId, CancellationToken token)
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
            return new EndpointInfo()
            {
                Endpoint = new PidIpcEndpoint(processId),
                ProcessId = processId,
                RuntimeInstanceCookie = processInfo?.RuntimeInstanceCookie ?? Guid.Empty,
                CommandLine = processInfo?.CommandLine,
                OperatingSystem = processInfo?.OperatingSystem,
                ProcessArchitecture = processInfo?.ProcessArchitecture
            };
        }

        public static async Task<EndpointInfo> FromIpcEndpointInfoAsync(IpcEndpointInfo info, ILogger logger, CancellationToken token)
        {
            var client = new DiagnosticsClient(info.Endpoint);

            ProcessInfo processInfo = null;
            try
            {
                logger?.LogError("[EndpointInfo][{pid}] Call {name}", info.ProcessId, nameof(DiagnosticsClient.GetProcessInfoAsync));
                // Primary motivation is to keep parity with the FromProcessId implementation,
                // which provides the additional process information because it already has
                // access to it.
                processInfo = await client.GetProcessInfoAsync(token);

                logger?.LogError("[EndpointInfo][{pid}] End {name}", info.ProcessId, nameof(DiagnosticsClient.GetProcessInfoAsync));

                Debug.Assert(info.ProcessId == unchecked((int)processInfo.ProcessId));
                Debug.Assert(info.RuntimeInstanceCookie == processInfo.RuntimeInstanceCookie);
            }
            catch (ServerErrorException ex)
            {
                logger?.LogError(ex, "[EndpointInfo][{pid}] Exception", info.ProcessId);
                // The runtime likely doesn't understand the GetProcessInfo command.
            }
            catch (TimeoutException ex)
            {
                logger?.LogError(ex, "[EndpointInfo][{pid}] Exception", info.ProcessId);
                // Runtime didn't respond within client timeout.
            }

            logger?.LogError("[EndpointInfo][{pid}] CommandLine: {commandLine}", info.ProcessId, processInfo?.CommandLine);

            return new EndpointInfo()
            {
                Endpoint = info.Endpoint,
                ProcessId = info.ProcessId,
                RuntimeInstanceCookie = info.RuntimeInstanceCookie,
                CommandLine = processInfo?.CommandLine,
                OperatingSystem = processInfo?.OperatingSystem,
                ProcessArchitecture = processInfo?.ProcessArchitecture
            };
        }

        public IpcEndpoint Endpoint { get; private set; }

        public int ProcessId { get; private set; }

        public Guid RuntimeInstanceCookie { get; private set; }

        public string CommandLine { get; private set; }

        public string OperatingSystem { get; private set; }

        public string ProcessArchitecture { get; private set; }

        internal string DebuggerDisplay => FormattableString.Invariant($"PID={ProcessId}, Cookie={RuntimeInstanceCookie}");
    }
}
