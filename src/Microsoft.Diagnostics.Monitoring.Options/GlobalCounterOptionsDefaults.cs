// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class GlobalCounterOptionsDefaults
    {
        public const float IntervalSeconds = 5.0f;

        public const int MaxHistograms = 20;

        public const int MaxTimeSeries = 1000;
    }
}
