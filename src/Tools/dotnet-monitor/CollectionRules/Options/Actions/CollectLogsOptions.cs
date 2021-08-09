// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    /// <summary>
    /// Options for the CollectLogs action.
    /// </summary>
    [DebuggerDisplay("CollectLogs")]
    internal sealed partial class CollectLogsOptions
    {
        [EnumDataType(typeof(LogLevel))]
        public LogLevel? LogLevel { get; set; }

        public Dictionary<string, LogLevel?> FilterSpecs { get; set; }

        public bool? UseAppFilters { get; set; }

        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        public TimeSpan? Duration { get; set; }

        [Required]
#if !UNITTEST
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
