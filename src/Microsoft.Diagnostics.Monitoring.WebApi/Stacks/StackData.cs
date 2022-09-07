// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    /// <summary>
    /// This data model is mostly 1:1 with the information that comes from the EventStacksPipeline.
    /// Note that most data is either ClassID's or mdToken information.
    /// </summary>
    internal sealed class StackResult
    {
        public List<Stack> Stacks { get; } = new();

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

        // Because it's possible that we never get the ClassID for a type, we separate Name information from the class.
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

        //It is possible that we are able to get the token but not the ClassID.

        public ulong ParentClass { get; set; }

        public uint ParentToken { get; set; }

        public ulong[] TypeArgs { get; set; }

        public ulong ModuleId { get; set; }
    }

    internal sealed class ModuleData
    {
        public string Name { get; set; }
    }

    internal sealed class StackFrame
    {
        public ulong FunctionId { get; set; }

        public ulong Offset { get; set; }
    }

    internal sealed class Stack
    {
        public List<StackFrame> Frames = new List<StackFrame>();

        public uint ThreadId { get; set; }
    }
}
