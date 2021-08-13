// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectTrace action.
    /// </summary>
    [DebuggerDisplay("CollectTrace")]
    internal sealed partial class CollectTraceOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_Profile))]
        [EnumDataType(typeof(TraceProfile))]
        public TraceProfile? Profile { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_MetricsIntervalSeconds))]
        [Range(ActionOptionsConstants.MetricsIntervalSeconds_MinValue, ActionOptionsConstants.MetricsIntervalSeconds_MaxValue)]
        [DefaultValue(CollectTraceOptionsDefaults.MetricsIntervalSeconds)]
        public int? MetricsIntervalSeconds { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_Providers))]
        public List<EventPipeProvider> Providers { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_RequestRundown))]
        [DefaultValue(CollectTraceOptionsDefaults.RequestRundown)]
        public bool? RequestRundown { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_BufferSizeMegabytes))]
        [Range(ActionOptionsConstants.BufferSizeMegabytes_MinValue, ActionOptionsConstants.BufferSizeMegabytes_MaxValue)]
        [DefaultValue(CollectTraceOptionsDefaults.BufferSizeMegabytes)]
        public int? BufferSizeMegabytes { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_Duration))]
        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        [DefaultValue(CollectTraceOptionsDefaults.Duration)]
        public TimeSpan? Duration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectTraceOptions_Egress))]
        [Required]
#if !UNITTEST && !SCHEMAGEN
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
