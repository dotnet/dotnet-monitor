// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
