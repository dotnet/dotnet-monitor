// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public record class CollectionRuleDetailedDescription : CollectionRuleDescription
    {
        /// <summary>
        /// The number of times the trigger has executed for a process in its lifetime.
        /// </summary>
        [JsonPropertyName("lifetimeOccurrences")]
        [Description("The number of times the trigger has executed for a process in its lifetime.")]
        public int LifetimeOccurrences { get; set; }

        /// <summary>
        /// The number of times the trigger has executed for a process in the current sliding window duration (as defined by Limits).
        /// </summary>
        [JsonPropertyName("slidingWindowOccurrences")]
        [Description("The number of times the trigger has executed for a process in the current sliding window duration (as defined by Limits).")]
        public int SlidingWindowOccurrences { get; set; }

        /// <summary>
        /// The number of times the trigger can execute for a process before being limited (as defined by Limits).
        /// </summary>
        [JsonPropertyName("actionCountLimit")]
        [Description("The number of times the trigger can execute for a process before being limited (as defined by Limits).")]
        public int ActionCountLimit { get; set; }

        /// <summary>
        /// The sliding window duration in which the actionCountLimit is the maximum number of occurrences (as defined by Limits).
        /// </summary>
        [JsonPropertyName("actionCountSlidingWindowDurationLimit")]
        [Description("The sliding window duration in which the actionCountLimit is the maximum number of occurrences (as defined by Limits).")]
        public TimeSpan? ActionCountSlidingWindowDurationLimit { get; set; }

        /// <summary>
        /// The amount of time that needs to pass before the slidingWindowOccurrences drops below the actionCountLimit
        /// </summary>
        [JsonPropertyName("slidingWindowDurationCountdown")]
        [Description("The amount of time that needs to pass before the slidingWindowOccurrences drops below the actionCountLimit")]
        public TimeSpan? SlidingWindowDurationCountdown { get; set; }

        /// <summary>
        /// The amount of time that needs to pass before the rule is finished
        /// </summary>
        [JsonPropertyName("ruleFinishedCountdown")]
        [Description("The amount of time that needs to pass before the rule is finished")]
        public TimeSpan? RuleFinishedCountdown { get; set; }
    }
}
