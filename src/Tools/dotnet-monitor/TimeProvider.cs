// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER
namespace System
{
    // The TimeProvider class is new to .NET 8 and not available in down-level versions.
    // The ISystemClock interfaces have been marked obsolete, being replaced by TimeProvider.
    // Stub out the minimal set of TimeProvider in order to replace ISystemClock usage for
    // both .NET 6 and .NET 8.
    public abstract class TimeProvider
    {
        public virtual DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }

        public static TimeProvider System { get; } = new SystemTimeProvider();

        private sealed class SystemTimeProvider : TimeProvider { }
    }
}
#endif
