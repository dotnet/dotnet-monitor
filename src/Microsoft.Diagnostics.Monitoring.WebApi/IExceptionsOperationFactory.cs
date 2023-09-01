// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Factory for creating operations that capture exceptions from the target.
    /// </summary>
    internal interface IExceptionsOperationFactory
    {
        /// <summary>
        /// Creates an operation that produces an exceptions artifact.
        /// </summary>
        IArtifactOperation Create(ExceptionFormat format, ExceptionsConfigurationSettings configuration);
    }
}
