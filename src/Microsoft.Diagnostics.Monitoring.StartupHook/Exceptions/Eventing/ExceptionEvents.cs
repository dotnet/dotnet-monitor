// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    internal static class ExceptionEvents
    {
        public static class ExceptionPayloads
        {
            public const int ExceptionId = 0;
            public const int ExceptionMessage = 1;
        }

        public static class ExceptionIdPayloads
        {
            public const int ExceptionId = 0;
            public const int ExceptionClassId = 1;
            public const int ThrowingMethodId = 2;
            public const int ILOffset = 3;
        }
    }
}
