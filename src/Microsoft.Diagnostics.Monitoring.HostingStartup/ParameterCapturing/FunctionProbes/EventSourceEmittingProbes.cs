// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class EventSourceEmittingProbes : IFunctionProbes
    {
        private readonly ParameterCapturingEventSource _eventSource;
        private readonly Guid _requestId;
        private readonly ObjectFormatterCache _objectFormatterCache;

        public EventSourceEmittingProbes(ParameterCapturingEventSource eventSource, Guid requestId, bool useDebuggerDisplayAttribute)
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
            try
            {
                return EnterProbeInternal(uniquifier, args);
            }
            catch (Exception ex)
            {
                _eventSource.FailedToCapture(_requestId, ParameterCapturingEvents.CapturingFailedReason.InternalError, ex.Message);
                return false;
            }
        }

        private bool EnterProbeInternal(ulong uniquifier, object[] args)
        {
            if (!_eventSource.IsEnabled())
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
                    string value;
                    if (!instrumentedMethod.SupportedParameters[i])
                    {
                        value = ObjectFormatter.Tokens.Unsupported;
                    }
                    else if (args[i] == null)
                    {
                        value = ObjectFormatter.Tokens.Null;
                    }
                    else
                    {
                        value = ObjectFormatter.FormatObject(_objectFormatterCache.GetFormatter(args[i].GetType()), args[i]);
                    }
                    resolvedArgs[i] = new ResolvedParameterInfo(
                        instrumentedMethod.MethodSignature.Parameters[i].Name,
                        instrumentedMethod.MethodSignature.Parameters[i].Type,
                        instrumentedMethod.MethodSignature.Parameters[i].TypeModuleName,
                        value,
                        instrumentedMethod.MethodSignature.Parameters[i].Attributes,
                        instrumentedMethod.MethodSignature.Parameters[i].IsByRef);
                }
            }

            _eventSource.CapturedParameters(
                _requestId,
                instrumentedMethod.MethodSignature.MethodName,
                instrumentedMethod.MethodSignature.ModuleName,
                instrumentedMethod.MethodSignature.TypeName,
                resolvedArgs);

            return true;
        }
    }
}
