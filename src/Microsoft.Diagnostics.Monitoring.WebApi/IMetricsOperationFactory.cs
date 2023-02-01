// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce metrics artifacts.
    /// </summary>
    internal interface IMetricsOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces a metrics artifact.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            MetricsPipelineSettings settings);
    }
}
