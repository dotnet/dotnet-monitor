﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    internal static class HighCPUOptionsDefaults
    {
        public const string SlidingWindowDuration = TriggerOptionsConstants.SlidingWindowDuration_Default;
        public const double GreaterThan = 50.0; // Arbitrary
    }
}
