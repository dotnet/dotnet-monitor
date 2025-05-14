// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing an entire collection rule.
    /// </summary>
    internal sealed partial class CollectionRuleOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Filters))]
        [ValidateEnumeratedItems]
        public List<ProcessFilterDescriptor> Filters { get; set; } = [];

#nullable disable
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Trigger))]
        [Required]
        [ValidateObjectMembers]
        public CollectionRuleTriggerOptions Trigger { get; set; }
#nullable enable

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Actions))]
        [ValidateEnumeratedItems]
        public List<CollectionRuleActionOptions> Actions { get; set; } = [];

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Limits))]
        [ValidateObjectMembers]
        public CollectionRuleLimitsOptions? Limits { get; set; }

        // Configuration bindig source generator doesn't support ValidationResult
        // error SYSLIB1100: The collection element type is not supported: 'System.Collections.Generic.List<ValidationResult>'.
        // (https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1100)
        // so use a custom type to hold the validation results and convert it to ValidationResult later.
        internal List<ErrorValidationResult> ErrorList { get; } = new List<ErrorValidationResult>();
    }

    struct ErrorValidationResult
    {
        public string Message { get; }
        public string MemberName { get; }

        public ErrorValidationResult(string message, string memberName)
        {
            Message = message;
            MemberName = memberName;
        }
    }
}
