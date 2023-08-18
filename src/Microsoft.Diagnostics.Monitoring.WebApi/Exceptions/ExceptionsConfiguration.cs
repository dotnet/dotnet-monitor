﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public List<ExceptionFilter> Include { get; set; } = new();

        /// <summary>
        /// The list of exception configurations that determine which exceptions should be shown.
        /// Each configuration is a logical OR, so if any of the configurations match, the exception isn't shown.
        /// </summary>
        [JsonPropertyName("exclude")]
        public List<ExceptionFilter> Exclude { get; set; } = new();

        private static bool CheckConfiguration(IExceptionInstance exception, List<ExceptionFilter> filterList, Func<ExceptionFilter, CallStackFrame, bool> evaluateFilterList)
        {
            var topFrame = exception.CallStack.Frames.FirstOrDefault();

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
            Func<ExceptionFilter, CallStackFrame, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool include = true;
                if (topFrame != null)
                {
                    CompareIncludeValues(configuration.MethodName, topFrame.MethodName, ref include);
                    CompareIncludeValues(configuration.ModuleName, topFrame.ModuleName, ref include);
                    CompareIncludeValues(configuration.ClassName, topFrame.ClassName, ref include);
                }

                CompareIncludeValues(configuration.ExceptionType, exception.TypeName, ref include);

                return include;
            };

            return CheckConfiguration(exception, Include, evaluateFilterList);
        }

        internal bool ShouldExclude(IExceptionInstance exception)
        {
            Func<ExceptionFilter, CallStackFrame, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool? exclude = null;
                if (topFrame != null)
                {
                    CompareExcludeValues(configuration.MethodName, topFrame.MethodName, ref exclude);
                    CompareExcludeValues(configuration.ModuleName, topFrame.ModuleName, ref exclude);
                    CompareExcludeValues(configuration.ClassName, topFrame.ClassName, ref exclude);
                    CompareExcludeValues(configuration.ExceptionType, exception.TypeName, ref exclude);
                }
                else
                {
                    CompareExcludeValues(configuration.ExceptionType, exception.TypeName, ref exclude);
                }

                return exclude ?? false;
            };

            return CheckConfiguration(exception, Exclude, evaluateFilterList);
        }

        private static void CompareIncludeValues(string configurationValue, string actualValue, ref bool include)
        {
            if (include && !string.IsNullOrEmpty(configurationValue))
            {
                include = CompareValues(configurationValue, actualValue);
            }
        }

        private static void CompareExcludeValues(string configurationValue, string actualValue, ref bool? exclude)
        {
            if (exclude != false && !string.IsNullOrEmpty(configurationValue))
            {
                exclude = CompareValues(configurationValue, actualValue);
            }
        }

        private static bool CompareValues(string configurationValue, string actualValue)
        {
            return actualValue.Equals(configurationValue, StringComparison.Ordinal);
        }
    }

    public class ExceptionFilter
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
