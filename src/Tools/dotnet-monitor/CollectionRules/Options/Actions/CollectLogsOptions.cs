// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectLogs action.
    /// </summary>
    [DebuggerDisplay("CollectLogs")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed partial record class CollectLogsOptions : BaseRecordOptions, IEgressProviderProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_DefaultLevel))]
        [EnumDataType(typeof(LogLevel))]
        [DefaultValue(CollectLogsOptionsDefaults.DefaultLevel)]
        public LogLevel? DefaultLevel { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_FilterSpecs))]
        public Dictionary<string, LogLevel?>? FilterSpecs { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_UseAppFilters))]
        [DefaultValue(CollectLogsOptionsDefaults.UseAppFilters)]
        public bool? UseAppFilters { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_Duration))]
        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        [DefaultValue(CollectLogsOptionsDefaults.Duration)]
        public TimeSpan? Duration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_Egress))]
        [Required(
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_NoDefaultEgressProvider))]
#if !UNITTEST && !SCHEMAGEN
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_Format))]
        [DefaultValue(CollectLogsOptionsDefaults.Format)]
        [EnumDataType(typeof(LogFormat))]
        public LogFormat? Format { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_ArtifactName))]
        public string? ArtifactName { get; set; }
    }
}
