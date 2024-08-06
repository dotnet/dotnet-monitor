// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal sealed class ExceptionsConfigurationSettings
    {
        public List<ExceptionFilterSettings> Include { get; set; } = new();

        public List<ExceptionFilterSettings> Exclude { get; set; } = new();

        private static bool CheckConfiguration(IExceptionInstance exception, List<ExceptionFilterSettings> filterList, Func<ExceptionFilterSettings, CallStackFrame?, bool> evaluateFilterList)
        {
            var topFrame = exception.CallStack?.Frames.FirstOrDefault();

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
            Func<ExceptionFilterSettings, CallStackFrame?, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool include = true;
                if (topFrame != null)
                {
                    CompareIncludeValues(configuration.MethodName, topFrame.MethodName, ref include);
                    CompareIncludeValues(configuration.ModuleName, topFrame.ModuleName, ref include);
                    CompareIncludeValues(configuration.TypeName, topFrame.TypeName, ref include);
                }

                CompareIncludeValues(configuration.ExceptionType, exception.TypeName, ref include);

                return include;
            };

            return CheckConfiguration(exception, Include, evaluateFilterList);
        }

        internal bool ShouldExclude(IExceptionInstance exception)
        {
            Func<ExceptionFilterSettings, CallStackFrame?, bool> evaluateFilterList = (configuration, topFrame) =>
            {
                bool? exclude = null;
                if (topFrame != null)
                {
                    CompareExcludeValues(configuration.MethodName, topFrame.MethodName, ref exclude);
                    CompareExcludeValues(configuration.ModuleName, topFrame.ModuleName, ref exclude);
                    CompareExcludeValues(configuration.TypeName, topFrame.TypeName, ref exclude);
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

        private static void CompareIncludeValues(string? configurationValue, string actualValue, ref bool include)
        {
            if (include && !string.IsNullOrEmpty(configurationValue))
            {
                include = CompareValues(configurationValue, actualValue);
            }
        }

        private static void CompareExcludeValues(string? configurationValue, string actualValue, ref bool? exclude)
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

    internal sealed class ExceptionFilterSettings
    {
        public string? MethodName { get; set; }

        public string? ExceptionType { get; set; }

        public string? TypeName { get; set; }

        public string? ModuleName { get; set; }
    }
}
