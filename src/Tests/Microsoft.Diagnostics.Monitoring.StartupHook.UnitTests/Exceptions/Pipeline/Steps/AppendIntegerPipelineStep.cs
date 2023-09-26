// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class AppendIntegerPipelineStep
    {
        private readonly int _id;
        private readonly List<int> _list;
        private readonly ExceptionPipelineDelegate _next;
        private readonly bool _shortCircuit;

        public AppendIntegerPipelineStep(ExceptionPipelineDelegate next, List<int> list, int id, bool shortCircuit)
        {
            _id = id;
            _list = list;
            _next = next;
            _shortCircuit = shortCircuit;
        }

        public void Invoke(Exception ex, ExceptionPipelineExceptionContext context)
        {
            _list.Add(_id);
            if (!_shortCircuit)
            {
                _next.Invoke(ex, context);
            }
        }

        public static Func<ExceptionPipelineDelegate, ExceptionPipelineDelegate> CreateBuilderStep(List<int> list, int id, bool shortCircuit = false)
        {
            return next => new AppendIntegerPipelineStep(next, list, id, shortCircuit).Invoke;
        }
    }
}
