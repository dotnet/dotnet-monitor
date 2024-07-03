// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Set of services provided by the monitoring tool. These are consumed by
    /// the REST Api.
    /// </summary>
    internal interface IDiagnosticServices
    {
        /// <summary>
        /// Returns running processes, optionally based on filter criteria.
        /// </summary>
        Task<IEnumerable<IProcessInfo>> GetProcessesAsync(DiagProcessFilter? processFilter, CancellationToken token);

        /// <summary>
        /// Returns a process based on a key. If no key is specified, the DefaultProcess configuration is used.
        /// </summary>
        /// <remarks>
        /// At the moment, we use this Api for all operations that require a single process, such as metrics or artifact collection urls with no pid.
        /// In the future, may want to update this to have an overload that also takes a DiagProcessFilter object, if different
        /// situations allow a different process object.
        /// </remarks>
        Task<IProcessInfo> GetProcessAsync(ProcessKey? processKey, CancellationToken token);
    }

    public interface IProcessInfo
    {
        IEndpointInfo EndpointInfo { get; }

        string? CommandLine { get; }

        public string? OperatingSystem { get; }

        public string? ProcessArchitecture { get; }

        string ProcessName { get; }
    }
}
