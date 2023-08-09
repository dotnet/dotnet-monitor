// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        internal bool ShouldInclude(IExceptionInstance exception)
        {
            var topFrame = exception.CallStack.Frames.Count > 0 ? exception.CallStack.Frames.First() : null; // is this safe?

            foreach (var configuration in Include)
            {
                bool include = true;
                if (topFrame != null)
                {
                    include = include ? CompareIncludeValues(configuration.MethodName, topFrame.MethodName) : include;
                    include = include ? CompareIncludeValues(configuration.ModuleName, topFrame.ModuleName) : include;
                    include = include ? CompareIncludeValues(configuration.ClassName, topFrame.ClassName) : include;
                }

                include = include ? CompareIncludeValues(configuration.ExceptionType, exception.TypeName) : include;

                if (include)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ShouldExclude(IExceptionInstance exception)
        {
            var topFrame = exception.CallStack.Frames.Any() ? exception.CallStack.Frames.First() : null;

            foreach (var configuration in Exclude)
            {
                bool exclude = false;
                if (topFrame != null)
                {
                    exclude = exclude ? exclude : CompareExcludeValues(configuration.MethodName, topFrame.MethodName);
                    exclude = exclude ? exclude : CompareExcludeValues(configuration.ModuleName, topFrame.ModuleName);
                    exclude = exclude ? exclude : CompareExcludeValues(configuration.ClassName, topFrame.ClassName);
                }

                exclude = exclude ? exclude : CompareExcludeValues(configuration.ExceptionType, exception.TypeName);

                if (exclude)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CompareIncludeValues(string configurationValue, string actualValue)
        {
            if (!string.IsNullOrEmpty(configurationValue))
            {
                return CompareValues(configurationValue, actualValue);
            }

            return true;
        }

        private static bool CompareExcludeValues(string configurationValue, string actualValue)
        {
            if (!string.IsNullOrEmpty(configurationValue))
            {
                return CompareValues(configurationValue, actualValue);
            }

            return false;
        }

        private static bool CompareValues(string configurationValue, string actualValue)
        {
            if (configurationValue.EndsWith('*'))
            {
                configurationValue = configurationValue.Remove(configurationValue.Length - 1);
                return actualValue.Contains(configurationValue, System.StringComparison.CurrentCultureIgnoreCase);
            }

            return actualValue.Equals(configurationValue, System.StringComparison.CurrentCultureIgnoreCase);
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
