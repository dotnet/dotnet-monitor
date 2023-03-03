// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
#else
namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
#endif
{
    internal static class ExceptionEvents
    {
        public const string SourceName = "Microsoft.Diagnostics.Monitoring.Exceptions";

        public static class EventIds
        {
            public const int ExceptionIdentifier = 1;
            public const int ExceptionInstance = 2;
            public const int ClassDescription = 3;
            public const int FunctionDescription = 4;
            public const int ModuleDescription = 5;
            public const int TokenDescription = 6;
            public const int Flush = 7;
        }

        public static class ExceptionInstancePayloads
        {
            public const int ExceptionId = 0;
            public const int ExceptionMessage = 1;
        }

        public static class ExceptionIdentifierPayloads
        {
            public const int ExceptionId = 0;
            public const int ExceptionClassId = 1;
            public const int ThrowingMethodId = 2;
            public const int ILOffset = 3;
        }
    }
}
