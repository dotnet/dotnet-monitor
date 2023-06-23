// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    /// <summary>
    /// Delegate representing an exception handler in the exception processing pipeline.
    /// </summary>
    internal delegate void ExceptionPipelineDelegate(Exception exception, ExceptionPipelineExceptionContext context);
}
