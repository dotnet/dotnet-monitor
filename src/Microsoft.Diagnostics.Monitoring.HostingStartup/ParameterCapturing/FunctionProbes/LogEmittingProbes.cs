// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class LogEmittingProbes : IFunctionProbes
    {
        private readonly ILogger _logger;

        public LogEmittingProbes(ILogger logger)
        {
            _logger = logger;
        }

        public void EnterProbe(ulong uniquifier, object[] args)
        {
            if (args == null ||
                !FunctionProbesStub.InstrumentedMethodCache.TryGetValue(uniquifier, out InstrumentedMethod instrumentedMethod) ||
                args.Length != instrumentedMethod.SupportedParameters.Length)
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

            _logger.Log(LogLevel.Information, instrumentedMethod.MethodWithParametersTemplateString, argValues);
            return;
        }
    }
}
