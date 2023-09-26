// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal static class CollectLiveMetricsOptionsExtensions
    {
        public static bool HasCustomConfiguration(this CollectLiveMetricsOptions options)
        {
            return options.Providers?.Length > 0 ||
                options.Meters?.Length > 0 ||
                options.IncludeDefaultProviders.HasValue;
        }

        public static bool GetIncludeDefaultProviders(this CollectLiveMetricsOptions options)
        {
            return options.IncludeDefaultProviders.GetValueOrDefault(CollectLiveMetricsOptionsDefaults.IncludeDefaultProviders);
        }

        public static TimeSpan GetDuration(this CollectLiveMetricsOptions options)
        {
            return options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLiveMetricsOptionsDefaults.Duration, CultureInfo.InvariantCulture));
        }
    }
}
