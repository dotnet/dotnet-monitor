// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public enum ProcessFilterKey
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterKey_ProcessId))]
        ProcessId,
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterKey_ProcessName))]
        ProcessName,
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterKey_CommandLine))]
        CommandLine,
    }

    public enum ProcessFilterType
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterType_Exact))]
        Exact,

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterType_Contains))]
        Contains,
    }

    public sealed class ProcessFilterOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterOptions_Filters))]
        public List<ProcessFilterDescriptor> Filters { get; set; } = [];
    }

    public sealed partial class ProcessFilterDescriptor
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_Key))]
        public ProcessFilterKey Key { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_Value))]
        public string? Value { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_MatchType))]
        [DefaultValue(ProcessFilterType.Exact)]
        public ProcessFilterType MatchType { get; set; } = ProcessFilterType.Exact;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_ProcessName))]
        public string? ProcessName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_ProcessId))]
        public string? ProcessId { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ProcessFilterDescriptor_CommandLine))]
        public string? CommandLine { get; set; }
    }

    partial class ProcessFilterDescriptor : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            if (string.IsNullOrWhiteSpace(CommandLine) && string.IsNullOrWhiteSpace(ProcessId) && string.IsNullOrWhiteSpace(ProcessName))
            {
                if (string.IsNullOrWhiteSpace(Value))
                {
                    results.Add(new ValidationResult(
                        string.Format(OptionsDisplayStrings.ErrorMessage_FilterValueMissing)));
                }
            }

            return results;
        }
    }
}
