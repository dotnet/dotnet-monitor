// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// We want to restrict the Prometheus scraping endpoint to only the /metrics call.
    /// To do this, we determine what port the request is on, and disallow other actions on the prometheus port.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class HostRestrictionAttribute : Attribute
    {
    }
}
