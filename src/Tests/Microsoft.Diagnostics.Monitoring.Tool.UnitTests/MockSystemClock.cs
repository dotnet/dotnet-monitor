﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal class MockSystemClock : ISystemClock
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public void Increment(TimeSpan timeSpan)
        {
            _utcNow += timeSpan;
        }

        public DateTimeOffset UtcNow => _utcNow;
    }
}
