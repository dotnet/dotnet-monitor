﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    public readonly struct InstrumentedMethod
    {
        public InstrumentedMethod(
            string methodWithParametersTemplateString,
            bool[] supportedParameters)
        {
            MethodWithParametersTemplateString = methodWithParametersTemplateString;
            SupportedParameters = supportedParameters;

            foreach (bool isParameterSupported in supportedParameters)
            {
                if (isParameterSupported)
                {
                    NumberOfSupportedParameters++;
                }
            }
        }

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
        /// 
        /// The number of format items equals NumberOfSupportedParameters.
        /// </summary>
        public string MethodWithParametersTemplateString { get; }
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
            string? templateString = PrettyPrinter.ConstructTemplateStringFromMethod(method, supportedParameters);
            if (templateString == null)
            {
                return false;
            }

            return _cache.TryAdd(
                method.GetFunctionId(),
                new InstrumentedMethod(
                    templateString,
                    supportedParameters));
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
