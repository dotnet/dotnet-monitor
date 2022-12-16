// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Interface representing a source of thrown exceptions.
    /// </summary>
    internal interface IExceptionSource
    {
        /// <summary>
        /// Event that is raised each time an exception is thrown.
        /// </summary>
        event EventHandler<Exception>? ExceptionThrown;
    }
}
