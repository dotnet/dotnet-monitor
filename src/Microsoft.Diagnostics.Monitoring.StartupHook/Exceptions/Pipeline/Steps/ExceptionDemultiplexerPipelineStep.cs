// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

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
            ExceptionPipelineExceptionContext innerContext = new(context.Timestamp, context.ActivityId, context.ActivityIdFormat, isInnerException: true);
            if (exception is AggregateException aggregateException)
            {
                // AggregateException will always pull the first exception out of the list of inner exceptions
                // and use that as its InnerException property. No need to report the InnerException property value.
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    Invoke(aggregateException.InnerExceptions[i], innerContext);
                }
            }
            else if (exception is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                // ReflectionTypeLoadException does not set InnerException. No need to report the InnerException property value.
                for (int i = 0; i < reflectionTypeLoadException.LoaderExceptions.Length; i++)
                {
                    Exception? loaderException = reflectionTypeLoadException.LoaderExceptions[i];
                    if (null != loaderException)
                    {
                        Invoke(loaderException, innerContext);
                    }
                }
            }
            else if (null != exception.InnerException)
            {
                Invoke(exception.InnerException, innerContext);
            }

            _next(exception, context);
        }
    }
}
