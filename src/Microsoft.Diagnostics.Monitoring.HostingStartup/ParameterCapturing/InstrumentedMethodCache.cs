// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    public readonly struct InstrumentedMethod
    {
        public InstrumentedMethod(
            MethodInfo methodInfo,
            string methodWithParametersFormatString,
            bool[] supportedParameters,
            bool hasImplicitThis,
            Type? declaringType,
            ParameterInfo[] explicitParameters)
        {
            MethodInfo = methodInfo;
            MethodWithParametersFormatString = methodWithParametersFormatString;
            SupportedParameters = supportedParameters;

            foreach (bool isParameterSupported in supportedParameters)
            {
                if (isParameterSupported)
                {
                    NumberOfSupportedParameters++;
                }
            }

            HasImplicitThis = hasImplicitThis;
            DeclaringType = declaringType;
            ExplicitParameters = explicitParameters;
        }

        /// <summary>
        /// The MethodInfo associated with this entry.
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// The total number of parameters (implicit and explicit) that are supported.
        /// </summary>
        public int NumberOfSupportedParameters { get; }

        /// <summary>
        /// An array containing whether each parameter (implicit and explicit) is supported.
        /// </summary>
        public bool[] SupportedParameters { get; }

        /// <summary>
        /// If the method has an implicit this.
        /// </summary>
        public bool HasImplicitThis { get; }

        /// <summary>
        /// If the method has an implicit this, the type associated with it.
        /// </summary>
        public Type? DeclaringType { get; }

        /// <summary>
        /// Contains all explicit parameters for a function (does not include the implicit this).
        /// </summary>
        public ParameterInfo[] ExplicitParameters { get; }

        /// <summary>
        /// A format string that contains the full method name with parameter names and
        /// format items for each supported parameter.
        /// 
        /// The number of format items equals NumberOfSupportedParameters.
        /// </summary>
        public string MethodWithParametersFormatString { get; }
    }

    public sealed class InstrumentedMethodCache
    {
        private readonly ConcurrentDictionary<ulong, InstrumentedMethod> _cache = new();

        public bool TryGetValue(ulong id, out InstrumentedMethod entry)
        {
            return _cache.TryGetValue(id, out entry);
        }

        public bool TryAdd(MethodInfo method, uint[] boxingTokens)
        {
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(boxingTokens);
            string? formattableString = PrettyPrinter.ConstructFormattableStringFromMethod(method, supportedParameters);
            if (formattableString == null)
            {
                return false;
            }

            _cache[method.GetFunctionId()] = new InstrumentedMethod(
                method,
                formattableString,
                supportedParameters,
                method.CallingConvention.HasFlag(CallingConventions.HasThis),
                method.DeclaringType,
                method.GetParameters());

            return true;
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
