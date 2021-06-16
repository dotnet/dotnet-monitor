// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi
#endif
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProcessFilterKey
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterKey_ProcessId))]
        ProcessId,
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterKey_ProcessName))]
        ProcessName,
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterKey_CommandLine))]
        CommandLine,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProcessFilterType
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterType_Exact))]
        Exact,

        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterType_Contains))]
        Contains,
    }

    public sealed class ProcessFilterOptions
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterOptions_Filters))]
        public List<ProcessFilterDescriptor> Filters { get; set; } = new List<ProcessFilterDescriptor>(0);
    }

    public sealed class ProcessFilterDescriptor
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterDescriptor_Key))]
        [Required]
        public ProcessFilterKey Key { get;set; }

        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterDescriptor_Value))]
        [Required]
        public string Value { get; set; }

        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_ProcessFilterDescriptor_MatchType))]
        [DefaultValue(ProcessFilterType.Exact)]
        public ProcessFilterType MatchType { get; set; } = ProcessFilterType.Exact;
    }
}
