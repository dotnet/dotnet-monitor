// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the AspNetRequestDuration trigger.
    /// </summary>
    internal sealed class AspNetRequestDurationOptions
    {
        [Required]
        public int RequestCount { get; set; }

        public TimeSpan? RequestDuration { get; set; }

        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        public string IncludePaths { get; set; }

        public string ExcludePaths { get; set; }
    }
}
