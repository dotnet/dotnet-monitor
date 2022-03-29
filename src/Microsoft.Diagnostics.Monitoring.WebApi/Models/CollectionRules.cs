// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class CollectionRules
    {
        /*
        /// <summary>
        /// Whether the trigger is currently enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool isEnabled { get; set; }
        */

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
        [JsonPropertyName("slidingWindowMaximumOccurrences")]
        public int SlidingWindowMaximumOccurrences { get; set; }

        /// <summary>
        /// Indicates what state the collection rule is in for the process.
        /// </summary>
        [JsonPropertyName("state")]
        public CollectionRulesState State { get; set; }
    }
}
