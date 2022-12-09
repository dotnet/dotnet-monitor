// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class RealSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public static ISystemClock Instance { get; } = new RealSystemClock();
    }
}
