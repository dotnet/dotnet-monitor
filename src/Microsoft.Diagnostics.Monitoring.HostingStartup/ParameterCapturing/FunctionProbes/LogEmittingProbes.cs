// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class LogEmittingProbes : IFunctionProbes
    {
        private readonly ParameterCapturingLogger _logger;

        public LogEmittingProbes(ParameterCapturingLogger logger)
        {
            _logger = logger;
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

            var methodCache = FunctionProbesStub.InstrumentedMethodCache;
            if (methodCache == null ||
                args == null ||
                !methodCache.TryGetValue(uniquifier, out InstrumentedMethod? instrumentedMethod) ||
                args.Length != instrumentedMethod?.SupportedParameters.Length)
            {
                return;
            }

            string[] argValues = new string[instrumentedMethod.NumberOfSupportedParameters];
            int fmtIndex = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (!instrumentedMethod.SupportedParameters[i])
                {
                    continue;
                }

                argValues[fmtIndex++] = PrettyPrinter.FormatObject(args[i]);
            }

            _logger.Log(instrumentedMethod.CaptureMode, instrumentedMethod.MethodWithParametersTemplateString, argValues);
        }
    }
}
