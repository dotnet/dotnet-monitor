// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public sealed class ExceptionsConfiguration
    {
        /// <summary>
        /// The list of exception configurations that determine which exceptions should be shown.
        /// Each configuration is a logical OR, so if any of the configurations match, the exception is shown.
        /// </summary>
        [JsonPropertyName("include")]
        public List<ExceptionFilter> Include { get; set; } = [];

        /// <summary>
        /// The list of exception configurations that determine which exceptions should be shown.
        /// Each configuration is a logical OR, so if any of the configurations match, the exception isn't shown.
        /// </summary>
        [JsonPropertyName("exclude")]
        public List<ExceptionFilter> Exclude { get; set; } = [];
    }

    public sealed class ExceptionFilter
    {
        [JsonPropertyName("exceptionType")]
        public string? ExceptionType { get; set; }

        [JsonPropertyName("moduleName")]
        public string? ModuleName { get; set; }

        [JsonPropertyName("typeName")]
        public string? TypeName { get; set; }

        [JsonPropertyName("methodName")]
        public string? MethodName { get; set; }
    }
}
