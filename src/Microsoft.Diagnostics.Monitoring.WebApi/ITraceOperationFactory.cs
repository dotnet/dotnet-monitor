// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;

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
            EventTracePipelineSettings settings,
            LogFormat format);
    }
}
