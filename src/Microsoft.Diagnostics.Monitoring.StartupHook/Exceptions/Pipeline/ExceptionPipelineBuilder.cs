// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    /// <summary>
    /// Builds a single exception pipeline handler from individual exception pipeline steps.
    /// </summary>
    internal sealed class ExceptionPipelineBuilder
    {
        private static readonly ExceptionPipelineDelegate Empty = _ => { };

        private List<Func<ExceptionPipelineDelegate, ExceptionPipelineDelegate>> _steps = new();

        public ExceptionPipelineBuilder Add(Func<ExceptionPipelineDelegate, ExceptionPipelineDelegate> step)
        {
            ArgumentNullException.ThrowIfNull(step);

            _steps.Add(step);
            return this;
        }

        public ExceptionPipelineDelegate Build()
        {
            ExceptionPipelineDelegate next = Empty;
            for (int index = _steps.Count - 1; index >= 0; index--)
            {
                next = _steps[index].Invoke(next);
            }
            return next;
        }
    }
}
