// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer
#endif
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProcessFilterKey
    {
        ProcessId,
        ProcessName,
        CommandLine,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProcessFilterType
    {
        [Display(Description = "Performs a case-insensitive string comparison.")]
        Exact,

        [Display(Description = "Performs a case-insensitive substring search.")]
        Contains,
    }

    public sealed class ProcessFilterOptions
    {
        [Display(Description = "Process filters used to determine the process when collecting metrics. All filters must match.")]
        public List<ProcessFilterDescriptor> Filters { get; set; } = new List<ProcessFilterDescriptor>(0);
    }

    public sealed class ProcessFilterDescriptor
    {
        [Display(Description = "The criteria used to compare against the target process.")]
        public ProcessFilterKey Key { get;set; }

        [Display(Description = "The value of the criteria used to compare against the target process.")]
        public string Value { get; set; }

        [Display(Description = "Type of match to use against the process criteria.")]
        public ProcessFilterType MatchType { get; set; }
    }
}
