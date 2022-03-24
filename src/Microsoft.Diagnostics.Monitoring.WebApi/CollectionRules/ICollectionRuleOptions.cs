// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Options for describing an entire collection rule.
    /// </summary>
    public class CollectionRuleOptions
    {
        public bool IsEnabled { get; set; }

        public List<ProcessFilterDescriptor> Filters { get; } = new List<ProcessFilterDescriptor>(0);

        [Required]
        public CollectionRuleTriggerOptions Trigger { get; set; }

        public List<CollectionRuleActionOptions> Actions { get; } = new List<CollectionRuleActionOptions>(0);

        public CollectionRuleLimitsOptions Limits { get; set; }
    }
}
