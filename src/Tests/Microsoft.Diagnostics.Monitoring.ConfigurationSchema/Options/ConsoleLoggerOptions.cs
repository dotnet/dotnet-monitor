// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal sealed class ConsoleLoggerOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_FormatterName))]
        [DefaultValue(ConsoleLoggerFormat.Simple)]
        public ConsoleLoggerFormat FormatterName { get; set; } = ConsoleLoggerFormat.Simple;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_LogToStandardErrorThreshold))]
        public LogLevel LogToStandardErrorThreshold { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_FormatterOptions))]
        public object? FormatterOptions { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleLoggerOptions_LogLevel))]
        public IDictionary<string, LogLevel?>? LogLevel { get; set; }
    }
}
