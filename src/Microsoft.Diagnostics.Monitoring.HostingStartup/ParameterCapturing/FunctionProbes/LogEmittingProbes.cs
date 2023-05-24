// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class LogEmittingProbes : IFunctionProbes
    {
        private readonly InstrumentedMethodCache _methodCache;
        private readonly ILogger _logger;

        public LogEmittingProbes(ILogger logger, InstrumentedMethodCache methodCache)
        {
            _logger = logger;
            _methodCache = methodCache;
        }

        public void EnterProbe(ulong uniquifier, object[] args)
        {
            if (_methodCache?.TryGetValue(uniquifier, out InstrumentedMethod instrumentedMethod) != true ||
                args?.Length != instrumentedMethod.SupportedParameters.Length)
            {
                return;
            }

            string[] argValues = new string[instrumentedMethod.NumberOfSupportedParameters];
            int fmtIndex = 0;
            for (int i = 0; i < args?.Length; i++)
            {
                if (!instrumentedMethod.SupportedParameters[i])
                {
                    continue;
                }

                argValues[fmtIndex++] = PrettyPrinter.SerializeObject(args[i]);
            }

            _logger.Log(LogLevel.Information, instrumentedMethod.MethodWithParametersFormatString, argValues);
            return;
        }
    }
}
