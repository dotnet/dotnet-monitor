// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ScopedEndpointInfo : IEndpointInfo
    {
#nullable disable
        private IEndpointInfo _endpointInfo;
#nullable restore

        public void Set(IEndpointInfo endpointInfo)
        {
            _endpointInfo = endpointInfo;
        }

        public int ProcessId => _endpointInfo.ProcessId;

        public Guid RuntimeInstanceCookie => _endpointInfo.RuntimeInstanceCookie;

        public string? CommandLine => _endpointInfo.CommandLine;

        public string? OperatingSystem => _endpointInfo.OperatingSystem;

        public string? ProcessArchitecture => _endpointInfo.ProcessArchitecture;

        public Version? RuntimeVersion => _endpointInfo.RuntimeVersion;

        IpcEndpoint IEndpointInfo.Endpoint => _endpointInfo.Endpoint;

        public IServiceProvider ServiceProvider => _endpointInfo.ServiceProvider;
    }
}
