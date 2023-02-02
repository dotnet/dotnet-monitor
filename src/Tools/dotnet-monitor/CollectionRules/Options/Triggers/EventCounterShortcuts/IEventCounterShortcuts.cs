// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts
{
    /// <summary>
    /// Options for IEventCounterShortcuts triggers.
    /// </summary>
    internal partial interface IEventCounterShortcuts
    {
        public double? GreaterThan { get; set; }

        public double? LessThan { get; set; }

        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
