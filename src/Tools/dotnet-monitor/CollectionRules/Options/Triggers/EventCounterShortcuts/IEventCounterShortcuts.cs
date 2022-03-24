// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts
{
    /// <summary>
    /// Options for the EventCounter trigger.
    /// </summary>
    internal partial interface IEventCounterShortcuts
    {
        public double? GreaterThan { get; set; }

        public double? LessThan { get; set; }

        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
