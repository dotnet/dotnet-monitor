// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
#endif
{
    /// <summary>
    /// Options for the EventCounter trigger.
    /// </summary>
    internal sealed partial class EventCounterOptions
    {
        [Required]
        public string ProviderName { get; set; }

        [Required]
        public string CounterName { get; set; }

        public double? GreaterThan { get; set; }

        public double? LessThan { get; set; }

        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        [Range(TriggerOptionsConstants.CounterFrequency_MinValue, TriggerOptionsConstants.CounterFrequency_MaxValue)]
        public int? Frequency { get; set; }
    }
}
