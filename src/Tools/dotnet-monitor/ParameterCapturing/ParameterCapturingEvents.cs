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
            public const int ServiceStateUpdate = 6;
            public const int ParametersCapturedStart = 7;
            public const int ParameterCaptured = 8;
            public const int ParametersCapturedStop = 9;
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
            InternalError,
            ProbeFaulted
        }

        public static class CapturingFailedPayloads
        {
            public const int RequestId = 0;
            public const int Reason = 1;
            public const int Details = 2;
        }

        public static class CapturedParametersStartPayloads
        {
            public const int RequestId = 0;
            public const int CaptureId = 1;
            public const int ActivityId = 2;
            public const int ActivityIdFormat = 3;
            public const int ThreadId = 4;
            public const int MethodName = 5;
            public const int MethodModuleName = 6;
            public const int MethodDeclaringTypeName = 7;
        }

        public static class CapturedParametersStopPayloads
        {
            public const int RequestId = 0;
            public const int CaptureId = 1;
        }

        public static class CapturedParameterPayloads
        {
            public const int RequestId = 0;
            public const int CaptureId = 1;
            public const int ParameterName = 2;
            public const int ParameterType = 3;
            public const int ParameterTypeModuleName = 4;
            public const int ParameterValue = 5;
            public const int ParameterValueEvaluationResult = 6;
            public const int ParameterAttributes = 7;
            public const int ParameterTypeIsByRef = 8;
        }

        public enum ParameterEvaluationResult : uint
        {
            Success = 0,
            IsNull,
            FailedEval,
            UnsupportedEval,
            EvalHasSideEffects
        }
    }
}
