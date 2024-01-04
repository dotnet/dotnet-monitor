// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal enum ParameterCaptureMode
    {
        Disallowed = 0,
        Inline,
        Background,
    }

    internal sealed class InstrumentedMethod
    {
        private static readonly string[] SystemTypePrefixes = { nameof(System), nameof(Microsoft) };

        public InstrumentedMethod(MethodInfo method, ParameterBoxingInstructions[] boxingInstructions)
        {
            FunctionId = method.GetFunctionId();
            SupportedParameters = BoxingInstructions.AreParametersSupported(boxingInstructions);
            MethodTemplateString = new MethodTemplateString(method);
            foreach (bool isParameterSupported in SupportedParameters)
            {
                if (isParameterSupported)
                {
                    NumberOfSupportedParameters++;
                }
            }

            CaptureMode = ComputeCaptureMode(method);
        }

        private static ParameterCaptureMode ComputeCaptureMode(MethodInfo method)
        {
            if (method.DeclaringType != null)
            {
                foreach (string typePrefix in SystemTypePrefixes)
                {
                    if (method.DoesBelongToType(typePrefix))
                    {
                        return ParameterCaptureMode.Background;
                    }
                }
            }

            return ParameterCaptureMode.Inline;
        }

        public ParameterCaptureMode CaptureMode { get; }

        /// <summary>
        /// The total number of parameters (implicit and explicit) that are supported.
        /// </summary>
        public int NumberOfSupportedParameters { get; }

        /// <summary>
        /// An array containing whether each parameter (implicit and explicit) is supported.
        /// </summary>
        public bool[] SupportedParameters { get; }

        /// <summary>
        /// A template string that contains the full method name with parameter names and
        /// format items for each supported parameter.
        /// </summary>
        public MethodTemplateString MethodTemplateString { get; }

        public ulong FunctionId { get; }
    }
}
