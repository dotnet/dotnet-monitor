// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Options for limiting the execution of a collection rule.
    /// </summary>
    public class CollectionRuleLimitsOptions
    {
        public int? ActionCount { get; set; }

        public TimeSpan? ActionCountSlidingWindowDuration { get; set; }

        public TimeSpan? RuleDuration { get; set; }
    }
}
