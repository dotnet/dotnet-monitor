// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Represents a source of thrown exceptions.
    /// </summary>
    internal abstract class ExceptionSourceBase
    {
        protected void RaiseExceptionThrown(Exception ex)
        {
            ExceptionThrown?.Invoke(this, ex);
        }

        /// <summary>
        /// Event that is raised each time an exception is thrown.
        /// </summary>
        public event EventHandler<Exception>? ExceptionThrown;
    }
}
