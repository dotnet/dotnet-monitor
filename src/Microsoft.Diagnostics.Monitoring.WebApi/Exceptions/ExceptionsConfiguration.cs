// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    public class ExceptionsConfiguration
    {
        /// <summary>
        /// The list of exception configurations that determine which exceptions should be shown.
        /// Each configuration is a logical OR, so if any of the configurations match, the exception is shown.
        /// </summary>
        [JsonPropertyName("include")]
        public List<ExceptionConfiguration> Include { get; set; } = new();

        /// <summary>
        /// The list of exception configurations that determine which exceptions should be shown.
        /// Each configuration is a logical OR, so if any of the configurations match, the exception isn't shown.
        /// </summary>
        [JsonPropertyName("exclude")]
        public List<ExceptionConfiguration> Exclude { get; set; } = new();

        /// <summary>
        /// By default, filters will check for a namespace.name match or a name match for ease-of-use.
        /// This toggle allows users to limit the filter to only check for an exact namespace.name match.
        /// </summary>
        [JsonPropertyName("allowSimplifiedNames")]
        public bool AllowSimplifiedNames { get; set; } = true;

        private static bool CheckConfiguration(IExceptionInstance exception, List<ExceptionConfiguration> filterList, Func<ExceptionConfiguration, CallStackFrame, bool> evaluateFilterList)
        {
            var topFrame = exception.CallStack.Frames.Any() ? exception.CallStack.Frames.First() : null;

            foreach (var configuration in filterList)
            {
                if (evaluateFilterList(configuration, topFrame))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ShouldInclude(IExceptionInstance exception)
        {
            Func<ExceptionConfiguration, CallStackFrame, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool include = true;
                if (topFrame != null)
                {
                    CompareIncludeValues(configuration.MethodName, GetSimplifiedName(topFrame.MethodName), topFrame.MethodName, ref include);
                    CompareIncludeValues(configuration.ModuleName, GetSimplifiedModuleName(topFrame.ModuleName), topFrame.ModuleName, ref include);
                    CompareIncludeValues(configuration.ClassName, GetSimplifiedName(topFrame.ClassName), topFrame.ClassName, ref include);
                }

                CompareIncludeValues(configuration.ExceptionType, GetSimplifiedName(exception.TypeName), exception.TypeName, ref include);

                return include;
            };

            return CheckConfiguration(exception, Include, evaluateFilterList);
        }

        internal bool ShouldExclude(IExceptionInstance exception)
        {
            Func<ExceptionConfiguration, CallStackFrame, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool? exclude = null;
                if (topFrame != null)
                {
                    CompareExcludeValues(configuration.MethodName, GetSimplifiedName(topFrame.MethodName), topFrame.MethodName, ref exclude);
                    CompareExcludeValues(configuration.ModuleName, GetSimplifiedModuleName(topFrame.ModuleName), topFrame.ModuleName, ref exclude);
                    CompareExcludeValues(configuration.ClassName, GetSimplifiedName(topFrame.ClassName), topFrame.ClassName, ref exclude);
                    CompareExcludeValues(configuration.ExceptionType, GetSimplifiedName(exception.TypeName), exception.TypeName, ref exclude);
                }
                else
                {
                    CompareExcludeValues(configuration.ExceptionType, GetSimplifiedName(exception.TypeName), exception.TypeName, ref exclude);
                }

                return exclude ?? false;
            };

            return CheckConfiguration(exception, Exclude, evaluateFilterList);
        }

        private static string GetSimplifiedName(string name)
        {
            var nestedIndex = name.LastIndexOf('+');
            if (nestedIndex != -1 && nestedIndex != name.Length - 1)
            {
                name = name.Substring(nestedIndex + 1);
            }

            var genericsIndex = name.LastIndexOf('`');
            if (genericsIndex != -1)
            {
                name = name.Substring(0, genericsIndex);
            }

            var lastPeriodIndex = name.LastIndexOf('.');
            if (lastPeriodIndex != -1 && lastPeriodIndex != name.Length - 1)
            {
                return name.Substring(lastPeriodIndex + 1);
            }

            return name;
        }

        private static string GetSimplifiedModuleName(string moduleName)
        {
            var lastTypeSeparatorIndex = moduleName.LastIndexOf('.');
            if (lastTypeSeparatorIndex != -1)
            {
                return GetSimplifiedName(moduleName.Substring(0, lastTypeSeparatorIndex)); // Do we always know there will be a . suffix?
            }

            return moduleName;
        }

        private void CompareIncludeValues(string configurationValue, string simpleValue, string fullValue, ref bool include)
        {
            if (include && !string.IsNullOrEmpty(configurationValue))
            {
                include = CompareValues(configurationValue, simpleValue, fullValue);
            }
        }

        private void CompareExcludeValues(string configurationValue, string simpleValue, string fullValue, ref bool? exclude)
        {
            if (exclude != false && !string.IsNullOrEmpty(configurationValue))
            {
                exclude = CompareValues(configurationValue, simpleValue, fullValue);
            }
        }

        private bool CompareValues(string configurationValue, string simpleValue, string fullValue)
        {
            if (AllowSimplifiedNames)
            {
                if (simpleValue.Equals(configurationValue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return fullValue.Equals(configurationValue, StringComparison.Ordinal);
        }
    }

    public class ExceptionConfiguration
    {
        /// <summary>
        /// The name of the top stack frame's method.
        /// </summary>
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        /// <summary>
        /// The type of the exception
        /// </summary>
        [JsonPropertyName("exceptionType")]
        public string ExceptionType { get; set; }

        /// <summary>
        /// The name of the top stack frame's class.
        /// </summary>
        [JsonPropertyName("className")]
        public string ClassName { get; set; }

        /// <summary>
        /// The name of the top stack frame's module.
        /// </summary>
        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; }
    }
}
