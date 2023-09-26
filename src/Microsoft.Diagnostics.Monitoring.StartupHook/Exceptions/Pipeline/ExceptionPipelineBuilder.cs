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
        private static readonly ExceptionPipelineDelegate Empty = (_, _) => { };

        private List<Func<ExceptionPipelineDelegate, ExceptionPipelineDelegate>> _steps = new();

        public ExceptionPipelineBuilder Add(Func<ExceptionPipelineDelegate, ExceptionPipelineDelegate> step)
        {
            ArgumentNullException.ThrowIfNull(step);

            _steps.Add(step);
            return this;
        }

        public ExceptionPipelineDelegate Build()
        {
            // DESIGN: Chaining delegates together could natively be done using multicast delegates,
            // however that would eliminate the ability for one pipeline step to short-circuit the
            // execution of the remaining steps, thus allowing exception filtering. Alternate designs
            // that allow for this would still require to not use multicast delegates or would need
            // to throw specialized exceptions.
            ExceptionPipelineDelegate next = Empty;
            for (int index = _steps.Count - 1; index >= 0; index--)
            {
                next = _steps[index].Invoke(next);
            }
            return next;
        }
    }
}
