// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    [Flags]
    public enum TraceProfile
    {
        Cpu = 0x1,
        Http = 0x2,
        Logs = 0x4,
        Metrics = 0x8,
        GcCollect = 0x10,
    }
}
