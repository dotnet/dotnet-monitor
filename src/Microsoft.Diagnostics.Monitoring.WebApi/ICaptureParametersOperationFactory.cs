// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that produce parameter capturing artifacts.
    /// </summary>
    internal interface ICaptureParametersOperationFactory
    {
        /// <summary>
        /// Creates an operation that captures parameters.
        /// </summary>
        IArtifactOperation Create(
            IEndpointInfo endpointInfo,
            CaptureParametersConfiguration configuration,
            TimeSpan duration,
            CapturedParameterFormat format);
    }
}
