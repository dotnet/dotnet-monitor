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
        IInProcessOperation Create(
            Guid requestId,
            IEndpointInfo endpointInfo,
            CaptureParametersConfiguration configuration,
            TimeSpan duration);

        /// <summary>
        /// Creates an operation that returns all captured parameters.
        /// </summary>
        IArtifactOperation CreateCapturedParameterFetcher(
            IEndpointInfo endpointInfo,
            Guid? requestId,
            CapturedParameterFormat format);
    }
}
