// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    /// <summary>
    /// This data model is mostly 1:1 with the information that comes from the EventStacksPipeline.
    /// Note that most data is either ClassID's or mdToken information.
    /// </summary>
    internal sealed class CallStackResult
    {
        public List<CallStack> Stacks { get; } = new();

        public NameCache NameCache { get; } = new NameCache();
    }

    internal sealed class CallStackFrame
    {
        public ulong FunctionId { get; set; }

        public uint MethodToken { get; set; }

        public Guid ModuleVersionId { get; set; }

        public ulong Offset { get; set; }
    }

    internal sealed class CallStack
    {
        public List<CallStackFrame> Frames = new List<CallStackFrame>();

        public uint ThreadId { get; set; }

        public string ThreadName { get; set; } = string.Empty;
    }
}
