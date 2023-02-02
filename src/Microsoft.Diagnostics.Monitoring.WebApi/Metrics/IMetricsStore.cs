// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Used to store metrics. A snapshot will be requested periodically.
    /// </summary>
    internal interface IMetricsStore : IDisposable
    {
        void AddMetric(ICounterPayload metric);

        Task SnapshotMetrics(Stream stream, CancellationToken token);

        void Clear();
    }
}
