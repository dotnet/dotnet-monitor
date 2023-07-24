// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal static class ParameterCapturingEvents
    {
        public const string SourceName = "Microsoft.Diagnostics.Monitoring.ParameterCapturing";

        public static class EventIds
        {
            public const int Flush = 1;

            public const int CapturingStart = 2;
            public const int CapturingStop = 3;
            public const int FailedToCapture = 4;
            public const int UnknownRequestId = 5;
            public const int ServiceStateChanged = 6;
        }


        public enum ServiceState : uint
        {
            NotStarted = 0,
            Running,
            Stopped,
            NotSupported,
            InternalError,
        }

        public static class CapturingActivityPayload
        {
            public const int RequestId = 0;
        }


        public static class UnknownRequestIdPayload
        {
            public const int RequestId = 0;
        }

        public static class ServiceStatePayload
        {
            public const int State = 0;
            public const int Details = 1;
        }

        public enum CapturingFailedReason : uint
        {
            UnresolvedMethods = 0,
            InvalidRequest,
            TooManyRequests,
            InternalError
        }

        public static class CapturingFailedPayloads
        {
            public const int RequestId = 0;
            public const int Reason = 1;
            public const int Details = 2;
        }
    }
}
