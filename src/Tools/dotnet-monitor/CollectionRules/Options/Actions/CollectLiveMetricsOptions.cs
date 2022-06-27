// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// Options for the CollectLiveMetrics action.
    /// </summary>
    [DebuggerDisplay("CollectLiveMetrics")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed partial record class CollectLiveMetricsOptions : BaseRecordOptions, IEgressProviderProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_Duration))]
        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        [DefaultValue(CollectLogsOptionsDefaults.Duration)]
        public TimeSpan? Duration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLogsOptions_Egress))]
        [Required(
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_NoDefaultEgressProvider))]
#if !UNITTEST && !SCHEMAGEN
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
