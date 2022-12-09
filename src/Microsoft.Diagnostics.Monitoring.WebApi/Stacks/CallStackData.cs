// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    internal sealed class NameCache
    {
        public Dictionary<ulong, ClassData> ClassData { get; } = new();
        public Dictionary<ulong, FunctionData> FunctionData { get; } = new();
        public Dictionary<ulong, ModuleData> ModuleData { get; } = new();
        public Dictionary<(ulong moduleId, ulong typeDef), TokenData> TokenData { get; } = new();
    }

    internal enum ClassFlags : uint
    {
        None = 0,
        Array,
        Composite,
        IncompleteData,
        Error = 0xff
    }

    internal sealed class ClassData
    {
        public ulong[] TypeArgs { get; set; }

        // We do not store the name of the class directly. The name can be retrieved from the TokenData.
        public uint Token { get; set; }

        public ulong ModuleId { get; set; }

        public ClassFlags Flags { get; set; }
    }

    internal sealed class TokenData
    {
        public uint OuterToken { get; set; }

        public string Name { get; set; }
    }

    internal sealed class FunctionData
    {
        public string Name { get; set; }

        /// <summary>
        /// ClassID of the containing class for this function. Note it's possible that the ClassID could not be retrieved by the profiler.
        /// In this case, only the token will be available.
        /// </summary>
        public ulong ParentClass { get; set; }

        /// <summary>
        /// If the ClassID could not be retrieved, the token can be used to get the name.
        /// </summary>
        public uint ParentToken { get; set; }

        public ulong[] TypeArgs { get; set; }

        public ulong ModuleId { get; set; }
    }

    internal sealed class ModuleData
    {
        public string Name { get; set; }
    }

    internal sealed class CallStackFrame
    {
        public ulong FunctionId { get; set; }

        public ulong Offset { get; set; }
    }

    internal sealed class CallStack
    {
        public List<CallStackFrame> Frames = new List<CallStackFrame>();

        public uint ThreadId { get; set; }

        public string ThreadName { get; set; }
    }
}
