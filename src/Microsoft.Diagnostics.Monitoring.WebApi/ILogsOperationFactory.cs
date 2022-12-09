// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce logs artifacts.
    /// </summary>
    internal interface ILogsOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces a logs artifact.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            EventLogsPipelineSettings settings,
            LogFormat format);
    }
}
