// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal interface ISlidingWindowDurationProperties
    {
        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
