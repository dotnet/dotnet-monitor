// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface IEndpointInfo
    {
        internal IpcEndpoint Endpoint { get; }

        int ProcessId { get; }

        Guid RuntimeInstanceCookie { get; }

        string? CommandLine { get; }

        string? OperatingSystem { get; }

        string? ProcessArchitecture { get; }

        Version? RuntimeVersion { get; }

        IServiceProvider ServiceProvider { get; }
    }

    /// <summary>
    /// Because IpcEndpoint may not be visible outside of this assembly (we have access to it here through InternalsVisibleTo), we
    /// create a base class that allows skipping the Endpoint property.
    /// </summary>
    internal abstract class EndpointInfoBase : IEndpointInfo
    {
        public virtual IpcEndpoint Endpoint
        {
            get => throw new NotImplementedException();
            protected set => throw new NotImplementedException();
        }

        public abstract int ProcessId { get; protected set; }
        public abstract Guid RuntimeInstanceCookie { get; protected set; }
        public abstract string? CommandLine { get; protected set; }
        public abstract string? OperatingSystem { get; protected set; }
        public abstract string? ProcessArchitecture { get; protected set; }

        public abstract Version? RuntimeVersion { get; protected set; }

        public abstract IServiceProvider ServiceProvider { get; protected set; }
    }

    public interface IEndpointInfoSource
    {
    }

    internal interface IEndpointInfoSourceInternal : IEndpointInfoSource
    {
        Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token);
    }
}
