// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if !NET8_0_OR_GREATER
#endif

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class MockTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Increment(TimeSpan timeSpan)
        {
            _utcNow += timeSpan;
        }
    }
}
