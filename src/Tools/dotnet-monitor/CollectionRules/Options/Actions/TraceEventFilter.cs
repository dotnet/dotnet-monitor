// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// A trace event filter.
    /// </summary>
    [DebuggerDisplay("TraceEventFilter")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed record class TraceEventFilter : BaseRecordOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventFilter_ProviderName))]
        [Required]
        public string ProviderName { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventFilter_EventName))]
        [Required]
        public string EventName { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventFilter_PayloadFilter))]
        public IDictionary<string, string>? PayloadFilter { get; set; }
    }
}
