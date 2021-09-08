// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Provides port information about the metrics URLs that were successfully bound.
    /// </summary>
    internal interface IMetricsPortsProvider
    {
        /// <summary>
        /// Get the ports of the metrics URLs.
        /// </summary>
        IEnumerable<int> MetricsPorts { get; }
    }
}
