// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class LogEmittingProbes : IFunctionProbes
    {
        private readonly ParameterCapturingLogger _logger;
        private readonly ObjectFormatterCache _objectFormatterCache;

        public LogEmittingProbes(ParameterCapturingLogger logger)
        {
            _logger = logger;
            _objectFormatterCache = new ObjectFormatterCache(useDebuggerDisplayAttribute: false);
        }

        public void CacheMethods(IList<MethodInfo> methods)
        {
            foreach (MethodInfo method in methods)
            {
                _objectFormatterCache.CacheMethodParameters(method);
            }
        }

        public void EnterProbe(ulong uniquifier, object[] args)
        {
            // We allow the instrumentation of system types, but these types can also be part of an ILogger implementation.
            // In addition, certain loggers don't log directly, but into a background thread.
            // We guard against reentrancy on the same thread.
            // All types that start with System & Microsoft do not invoke ILogger directly. Rather, they queue a message to a dedicated background thread.
            // All probes that are triggered from the dedicated background thread do not log.
            // All probes from the known console logger processor thread do not log.

            // Possible additional guards in the future:
            // If we instrument Modules instead of specific methods, we may need to exclude certain types to prevent noise. (such as System.String)
            // If other custom loggers create background threads, we may need a way to specify exclusions for those threads.

            if (!_logger.ShouldLog())
            {
                return;
            }

            FunctionProbesState? state = FunctionProbesStub.State;
            if (state == null ||
                args == null ||
                !state.InstrumentedMethods.TryGetValue(uniquifier, out InstrumentedMethod? instrumentedMethod) ||
                args.Length != instrumentedMethod?.SupportedParameters.Length)
            {
                return;
            }

            if (instrumentedMethod.CaptureMode == ParameterCaptureMode.Disallowed)
            {
                return;
            }

            string[] argValues;
            if (args.Length == 0)
            {
                argValues = Array.Empty<string>();
            }
            else
            {
                argValues = new string[args.Length];
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
                    argValues[i] = value;
                }
            }

            _logger.Log(instrumentedMethod.CaptureMode, instrumentedMethod.MethodTemplateString, argValues);
        }
    }
}
