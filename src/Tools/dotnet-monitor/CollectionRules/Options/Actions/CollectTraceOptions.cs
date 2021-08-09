// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectTrace action.
    /// </summary>
    [DebuggerDisplay("CollectTrace")]
    internal sealed partial class CollectTraceOptions
    {
        [EnumDataType(typeof(TraceProfile))]
        public TraceProfile? Profile { get; set; }

        [Range(ActionOptionsConstants.MetricsIntervalSeconds_MinValue, ActionOptionsConstants.MetricsIntervalSeconds_MaxValue)]
        public int? MetricsIntervalSeconds { get; set; }

        public List<EventPipeProvider> Providers { get; set; }

        public bool? RequestRundown { get; set; }

        [Range(ActionOptionsConstants.BufferSizeMegabytes_MinValue, ActionOptionsConstants.BufferSizeMegabytes_MaxValue)]
        public int? BufferSizeMegabytes { get; set; }

        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        public TimeSpan? Duration { get; set; }

        [Required]
#if !UNITTEST
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
