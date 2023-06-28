// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    /// <summary>
    /// An exception pipeline step that will only allow an exception to be
    /// processed further if it was not observed previously.
    /// </summary>
    internal sealed class FilterRepeatExceptionPipelineStep
    {
        // This is not static so that instances of this action can be used
        // in multiple pipelines that may process the same exception.
        private readonly object _firstOccurrenceKey = new();
        private readonly ExceptionPipelineDelegate _next;

        public FilterRepeatExceptionPipelineStep(ExceptionPipelineDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);

            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (IsFirstObservance(exception))
            {
                MarkObservance(exception);

                _next(exception, context);
            }
        }

        private bool IsFirstObservance(Exception ex)
        {
            return !ex.Data.Contains(_firstOccurrenceKey);
        }

        private void MarkObservance(Exception ex)
        {
            ex.Data.Add(_firstOccurrenceKey, null);
        }
    }
}
