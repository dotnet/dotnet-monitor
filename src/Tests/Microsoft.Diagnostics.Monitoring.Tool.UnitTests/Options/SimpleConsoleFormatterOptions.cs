// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
{
    internal sealed class SimpleConsoleFormatterOptions
    {
#if NET5_0_OR_GREATER
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SimpleConsoleFormatterOptions_ColorBehavior))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LoggerColorBehavior ColorBehavior { get; set; }
#endif

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SimpleConsoleFormatterOptions_SingleLine))]
        public bool SingleLine { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleFormatterOptions_IncludeScopes))]
        public bool IncludeScopes { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleFormatterOptions_TimestampFormat))]
        [DefaultValue(null)]
        public string TimestampFormat { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ConsoleFormatterOptions_UseUtcTimestamp))]
        [DefaultValue(false)]
        public bool UseUtcTimestamp { get; set; }
    }
}
