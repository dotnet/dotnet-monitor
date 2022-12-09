﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal sealed class JsonConsoleFormatterOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_JsonConsoleFormatterOptions_WriterOptions))]
        public JsonWriterOptions JsonWriterOptions { get; set; }

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
