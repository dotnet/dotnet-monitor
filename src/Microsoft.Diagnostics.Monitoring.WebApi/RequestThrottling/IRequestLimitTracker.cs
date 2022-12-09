// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IRequestLimitTracker
    {
        IDisposable Increment(string key, out bool allowOperation);
    }
}
