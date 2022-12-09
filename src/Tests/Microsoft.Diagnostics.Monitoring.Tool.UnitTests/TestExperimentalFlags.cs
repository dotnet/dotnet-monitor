// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class TestExperimentalFlags : Microsoft.Diagnostics.Monitoring.WebApi.IExperimentalFlags
    {
        public bool IsCallStacksEnabled { get; set; }
    }
}
