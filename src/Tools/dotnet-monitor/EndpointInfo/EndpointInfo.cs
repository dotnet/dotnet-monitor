// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class EndpointInfo : EndpointInfoBase
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
            // Note that currently we use RuntimeInstanceId == Guid.Empty as a means of determining
            // if an app is running as 3.1
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

        public static async Task<EndpointInfo> FromIpcEndpointInfoAsync(IpcEndpointInfo info, CancellationToken token)
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

        public override IpcEndpoint Endpoint { get; protected set; }

        public override int ProcessId { get; protected set; }

        public override Guid RuntimeInstanceCookie { get; protected set; }

        public override string CommandLine { get; protected set; }

        public override string OperatingSystem { get; protected set; }

        public override string ProcessArchitecture { get; protected set; }

        internal string DebuggerDisplay => FormattableString.Invariant($"PID={ProcessId}, Cookie={RuntimeInstanceCookie}");
    }
}
