// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal static class StackEvents
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
            public const int FunctionIds = 1;
            public const int IpOffsets = 2;
        }

        public static class FunctionDescPayloads
        {
            public const int FunctionId = 0;
            public const int ClassId = 1;
            public const int ClassToken = 2;
            public const int ModuleId = 3;
            public const int Name = 4;
            public const int TypeArgs = 5;
        }

        public static class ClassDescPayloads
        {
            public const int ClassId = 0;
            public const int ModuleId = 1;
            public const int Token = 2;
            public const int Flags = 3;
            public const int TypeArgs = 4;
        }

        public static class ModuleDescPayloads
        {
            public const int ModuleId = 0;
            public const int Name = 1;
        }
        public static class TokenDescPayloads
        {
            public const int ModuleId = 0;
            public const int Token = 1;
            public const int OuterToken = 2;
            public const int Name = 3;
        }

        public static class EndPayloads
        {
            public const int Unused = 0;
        }
    }
}
