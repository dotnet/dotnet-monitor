// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal sealed class ConsoleLoggerOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_FormatterName))]
        [DefaultValue(ConsoleLoggerFormat.Simple)]
        public ConsoleLoggerFormat FormatterName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_LogToStandardErrorThreshold))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel LogToStandardErrorThreshold { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_FormatterOptions))]
        public object FormatterOptions { get; set; }
    }
}
