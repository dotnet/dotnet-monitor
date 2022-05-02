// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public record class CollectionRuleDescription
    {
        /*
        /// <summary>
        /// Whether the trigger is currently enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool isEnabled { get; set; }
        */

        /// <summary>
        /// Indicates what state the collection rule is in for the process.
        /// </summary>
        [JsonPropertyName("state")]
        public CollectionRuleState State { get; set; }

        /// <summary>
        /// Human-readable explanation for the current state of the collection rule.
        /// </summary>
        [JsonPropertyName("stateReason")]
        public string StateReason { get; set; }

        /// <summary>
        /// The number of times the trigger has executed for a process in its lifetime.
        /// </summary>
        [JsonPropertyName("lifetimeOccurrences")]
        public int LifetimeOccurrences { get; set; }

        /// <summary>
        /// The number of times the trigger has executed for a process in the current sliding window duration (as defined by Limits).
        /// </summary>
        [JsonPropertyName("slidingWindowOccurrences")]
        public int SlidingWindowOccurrences { get; set; }

        /// <summary>
        /// The number of times the trigger can execute for a process before being limited -> (as defined by Limits).
        /// </summary>
        [JsonPropertyName("actionCountLimit")]
        public int ActionCountLimit { get; set; }

        /// <summary>
        /// The sliding window duration in which the ActionCount is the maximum number of occurrences -> (as defined by Limits).
        /// </summary>
        [JsonPropertyName("actionCountSlidingWindowDurationLimit")]
        public TimeSpan? ActionCountSlidingWindowDurationLimit { get; set; }

        /// <summary>
        /// The amount of time that needs to pass before the slidingWindowOccurrences drops below the actionCount
        /// </summary>
        [JsonPropertyName("slidingWindowDurationCountdown")]
        public TimeSpan? SlidingWindowDurationCountdown { get; set; }

        /// <summary>
        /// The amount of time that needs to pass before the rule is finished
        /// </summary>
        [JsonPropertyName("ruleFinishedCountdown")]
        public TimeSpan? RuleFinishedCountdown { get; set; }
    }
}
