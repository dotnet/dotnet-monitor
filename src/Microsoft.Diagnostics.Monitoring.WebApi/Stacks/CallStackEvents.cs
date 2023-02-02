// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal static class CallStackEvents
    {
        public const string Provider = "DotnetMonitorStacksEventProvider";

        public const TraceEventID Callstack = (TraceEventID)1;
        public const TraceEventID FunctionDesc = (TraceEventID)2;
        public const TraceEventID ClassDesc = (TraceEventID)3;
        public const TraceEventID ModuleDesc = (TraceEventID)4;
        public const TraceEventID TokenDesc = (TraceEventID)5;
        public const TraceEventID End = (TraceEventID)6;

        public static class CallstackPayloads
        {
            public const int ThreadId = 0;
            public const int ThreadName = 1;
            public const int FunctionIds = 2;
            public const int IpOffsets = 3;
        }

        public static class EndPayloads
        {
            public const int Unused = 0;
        }
    }
}
