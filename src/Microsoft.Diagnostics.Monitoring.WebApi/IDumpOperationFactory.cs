// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce dump artifacts.
    /// </summary>
    internal interface IDumpOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces a dump artifact.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            DumpType dumpType);
    }
}
