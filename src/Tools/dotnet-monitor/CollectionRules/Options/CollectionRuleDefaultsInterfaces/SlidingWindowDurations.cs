// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal interface SlidingWindowDurations
    {
        /*[Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]*/
        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
