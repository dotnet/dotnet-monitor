// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
{
    internal sealed class EventSourceEmittingProbes : IFunctionProbes
    {
        private readonly AsyncParameterCapturingEventSource _eventSource;
        private readonly Guid _requestId;
        private readonly ObjectFormatterCache _objectFormatterCache;

        public EventSourceEmittingProbes(AsyncParameterCapturingEventSource eventSource, Guid requestId, bool useDebuggerDisplayAttribute)
        {
            _eventSource = eventSource;
            _requestId = requestId;
            _objectFormatterCache = new ObjectFormatterCache(useDebuggerDisplayAttribute);
        }

        public void CacheMethods(IList<MethodInfo> methods)
        {
            foreach (MethodInfo method in methods)
            {
                _objectFormatterCache.CacheMethodParameters(method);
            }
        }

        public bool EnterProbe(ulong uniquifier, object[] args)
        {
            if (!_eventSource.IsEnabled)
            {
                return false;
            }

            FunctionProbesState? state = FunctionProbesStub.State;
            if (state == null ||
                args == null ||
                !state.InstrumentedMethods.TryGetValue(uniquifier, out InstrumentedMethod? instrumentedMethod) ||
                args.Length != instrumentedMethod?.SupportedParameters.Length ||
                args.Length != instrumentedMethod?.MethodSignature.Parameters.Count)
            {
                return false;
            }

            if (instrumentedMethod.CaptureMode == ParameterCaptureMode.Disallowed)
            {
                return false;
            }

            ResolvedParameterInfo[] resolvedArgs = [];
            if (args.Length > 0)
            {
                resolvedArgs = new ResolvedParameterInfo[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    ObjectFormatterResult evalResult;
                    if (!instrumentedMethod.SupportedParameters[i])
                    {
                        evalResult = ObjectFormatterResult.Unsupported;
                    }
                    else if (args[i] == null)
                    {
                        evalResult = ObjectFormatterResult.Null;
                    }
                    else
                    {
                        evalResult = ObjectFormatter.FormatObject(_objectFormatterCache.GetFormatter(args[i].GetType()), args[i], FormatSpecifier.NoQuotes);
                    }
                    resolvedArgs[i] = new ResolvedParameterInfo(
                        instrumentedMethod.MethodSignature.Parameters[i].Name,
                        instrumentedMethod.MethodSignature.Parameters[i].Type,
                        instrumentedMethod.MethodSignature.Parameters[i].TypeModuleName,
                        evalResult,
                        instrumentedMethod.MethodSignature.Parameters[i].Attributes,
                        instrumentedMethod.MethodSignature.Parameters[i].IsByRef);
                }
            }

            _eventSource.OnCapturedParameters(
                _requestId,
                instrumentedMethod.MethodSignature.MethodName,
                instrumentedMethod.MethodSignature.ModuleName,
                instrumentedMethod.MethodSignature.TypeName,
                resolvedArgs);

            return true;
        }
    }
}
