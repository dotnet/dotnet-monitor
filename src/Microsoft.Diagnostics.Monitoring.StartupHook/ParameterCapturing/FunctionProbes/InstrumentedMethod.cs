// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
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
            MethodSignature = new MethodSignature(method);
            foreach (bool isParameterSupported in SupportedParameters)
            {
                if (isParameterSupported)
                {
                    NumberOfSupportedParameters++;
                }
            }

            CaptureMode = ComputeCaptureMode(method);

            // Hold a reference to the method to ensure we keep the assembly it belongs to from being unloaded
            // while we are instrumenting it.
            Method = method;
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

        public MethodInfo Method { get; private set; }

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
        /// Information about the method (name, parameter types, parameter names).
        /// </summary>
        public MethodSignature MethodSignature { get; }

        public ulong FunctionId { get; }
    }
}
