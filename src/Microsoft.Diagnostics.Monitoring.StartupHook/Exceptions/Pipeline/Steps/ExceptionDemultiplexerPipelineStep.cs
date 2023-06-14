// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    /// <summary>
    /// An exception pipeline step that reports the related exception instances of
    /// the provided exception, namely the inner exception instances, as well as the main
    /// exception itself.
    /// </summary>
    internal class ExceptionDemultiplexerPipelineStep
    {
        private readonly ExceptionPipelineDelegate _next;

        public ExceptionDemultiplexerPipelineStep(ExceptionPipelineDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);

            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
        {
            ExceptionPipelineExceptionContext innerContext = new(context.Timestamp, isInnerException: true);

            if (null != exception.InnerException)
            {
                Invoke(exception.InnerException, innerContext);
            }

            if (exception is AggregateException aggregateException)
            {
                int startingIndex = 0;
                // Skip the first exception since it was already reported for the InnerException property
                // AggregateException will always pull the first exception out of the list of inner exceptions
                // and use that as its InnerException property.
                if (null != exception.InnerException)
                {
                    Debug.Assert(aggregateException.InnerExceptions.Count == 0 || aggregateException.InnerExceptions[0] == exception.InnerException);

                    startingIndex = 1;
                }

                for (int i = startingIndex; i < aggregateException.InnerExceptions.Count; i++)
                {
                    Invoke(aggregateException.InnerExceptions[i], innerContext);
                }
            }

            _next(exception, context);
        }
    }
}
