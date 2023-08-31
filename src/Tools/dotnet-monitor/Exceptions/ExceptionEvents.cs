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
            public const int ExceptionGroup = 1;
            public const int ExceptionInstance = 2;
            public const int ClassDescription = 3;
            public const int FunctionDescription = 4;
            public const int ModuleDescription = 5;
            public const int TokenDescription = 6;
            public const int Flush = 7;
            public const int StackFrameDescription = 8;
            public const int ExceptionInstanceUnhandled = 9;
        }

        public static class ExceptionInstancePayloads
        {
            public const int ExceptionId = 0;
            public const int ExceptionGroupId = 1;
            public const int ExceptionMessage = 2;
            public const int StackFrameIds = 3;
            public const int Timestamp = 4;
            public const int InnerExceptionIds = 5;
            public const int ActivityId = 6;
            public const int ActivityIdFormat = 7;
        }

        public static class ExceptionInstanceUnhandledPayloads
        {
            public const int ExceptionId = 0;
        }

        public static class ExceptionGroupPayloads
        {
            public const int ExceptionGroupId = 0;
            public const int ExceptionClassId = 1;
            public const int ThrowingMethodId = 2;
            public const int ILOffset = 3;
        }

        public static class StackFrameIdentifierPayloads
        {
            public const int StackFrameId = 0;
            public const int FunctionId = 1;
            public const int ILOffset = 2;
        }
    }
}
