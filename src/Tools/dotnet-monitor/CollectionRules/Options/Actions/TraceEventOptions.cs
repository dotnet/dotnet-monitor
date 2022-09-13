// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tracing;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectTrace action.
    /// </summary>
    [DebuggerDisplay("TraceEvent")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed record class TraceEventOptions : BaseRecordOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventOptions_ProviderName))]
        [Required]
        public string ProviderName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventOptions_EventName))]
        [Required]
        public string EventName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_TraceEventOptions_Opcode))]
        [EnumDataType(typeof(TraceEventOpcode))]
        public TraceEventOpcode? Opcode { get; set; }
    }
}
