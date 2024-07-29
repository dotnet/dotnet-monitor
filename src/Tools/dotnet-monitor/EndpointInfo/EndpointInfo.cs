// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class EndpointInfo : EndpointInfoBase
    {
        public static async Task<EndpointInfo> FromProcessIdAsync(int processId, IServiceProvider serviceProvider, CancellationToken token)
        {
            var client = new DiagnosticsClient(processId);

            ProcessInfo? processInfo = null;
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

            _ = TryParseVersion(processInfo?.ClrProductVersionString, out Version? runtimeVersion);

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
                ProcessArchitecture = processInfo?.ProcessArchitecture,
                RuntimeVersion = runtimeVersion,
                ServiceProvider = serviceProvider
            };
        }

        public static async Task<EndpointInfo> FromIpcEndpointInfoAsync(IpcEndpointInfo info, IServiceProvider serviceProvider, CancellationToken token)
        {
            var client = new DiagnosticsClient(info.Endpoint);

            ProcessInfo? processInfo = null;
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

            _ = TryParseVersion(processInfo?.ClrProductVersionString, out Version? runtimeVersion);

            return new EndpointInfo()
            {
                Endpoint = info.Endpoint,
                ProcessId = info.ProcessId,
                RuntimeInstanceCookie = info.RuntimeInstanceCookie,
                CommandLine = processInfo?.CommandLine,
                OperatingSystem = processInfo?.OperatingSystem,
                ProcessArchitecture = processInfo?.ProcessArchitecture,
                RuntimeVersion = runtimeVersion,
                ServiceProvider = serviceProvider
            };
        }

        private static bool TryParseVersion(string? versionString, [NotNullWhen(true)] out Version? version)
        {
            version = null;
            if (string.IsNullOrEmpty(versionString))
            {
                return false;
            }

            // The version is of the SemVer2 form: <major>.<minor>.<patch>[-<prerelease>][+<metadata>]
            // Remove the prerelease and metadata version information before parsing.

            ReadOnlySpan<char> versionSpan = versionString;
            int metadataIndex = versionSpan.IndexOf('+');
            if (-1 == metadataIndex)
            {
                metadataIndex = versionSpan.Length;
            }

            ReadOnlySpan<char> noMetadataVersion = versionSpan[..metadataIndex];
            int prereleaseIndex = noMetadataVersion.IndexOf('-');
            if (-1 == prereleaseIndex)
            {
                prereleaseIndex = metadataIndex;
            }

            return Version.TryParse(noMetadataVersion[..prereleaseIndex], out version);
        }

#nullable disable
        public override IpcEndpoint Endpoint { get; protected set; }
#nullable restore

        public override int ProcessId { get; protected set; }

        public override Guid RuntimeInstanceCookie { get; protected set; }

        public override string? CommandLine { get; protected set; }

        public override string? OperatingSystem { get; protected set; }

        public override string? ProcessArchitecture { get; protected set; }

        public override Version? RuntimeVersion { get; protected set; }

#nullable disable
        public override IServiceProvider ServiceProvider { get; protected set; }
#nullable restore

        internal string DebuggerDisplay => FormattableString.Invariant($"PID={ProcessId}, Cookie={RuntimeInstanceCookie}");
    }
}
