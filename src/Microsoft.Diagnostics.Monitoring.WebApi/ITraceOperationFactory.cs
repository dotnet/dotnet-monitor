// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce trace artifacts.
    /// </summary>
    internal interface ITraceOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces a trace artifact.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration);

        /// <summary>
        /// Creates an operation that produces a trace artifact and supports a stopping event.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration,
            string providerName,
            string eventName,
            IDictionary<string, string>? payloadFilter);
    }
}
