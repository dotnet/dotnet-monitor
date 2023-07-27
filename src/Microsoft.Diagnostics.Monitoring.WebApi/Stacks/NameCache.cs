// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
#endif
{
    internal sealed class NameCache
    {
        public ConcurrentDictionary<ulong, ClassData> ClassData { get; } = new();
        public ConcurrentDictionary<ulong, FunctionData> FunctionData { get; } = new();
        public ConcurrentDictionary<ulong, ModuleData> ModuleData { get; } = new();
        public ConcurrentDictionary<ModuleScopedToken, TokenData> TokenData { get; } = new();
    }

    internal enum ClassFlags : uint
    {
        None = 0,
        Array,
        Composite,
        IncompleteData,
        Error = 0xff
    }

    /// <param name="Token">The metadata token of the class.</param>
    /// <param name="ModuleId">The identifier of the module that contains the class.</param>
    /// <param name="Flags">The flags for the class.</param>
    /// <param name="TypeArgs">The class identifiers of the generic type arguments of the class.</param>
    /// <remarks>
    /// The name of the class can be retrieved from the corresponding <see cref="TokenData"/>.
    /// </remarks>
    internal sealed record class ClassData(uint Token, ulong ModuleId, ClassFlags Flags, ulong[] TypeArgs);

    /// <param name="Name">The name of the token.</param>
    /// <param name="TokenNamespace">The namespace of the token.</param>
    /// <param name="OuterToken">The metadata token of the parent container.</param>
    [DebuggerDisplay("{Name}")]
    internal sealed record class TokenData(string Name, string TokenNamespace, uint OuterToken);

    /// <param name="Name">The name of the function.</param>
    /// <param name="ParentClass">The parent class identifier of the function.</param>
    /// <param name="ParentToken">The parent metadata token of the function.</param>
    /// <param name="ModuleId">The identifier of the module that contains the function.</param>
    /// <param name="TypeArgs">The class identifiers of the generic type arguments of the function.</param>
    /// <param name="ParameterTypes">The class identifiers of the parameter types of the function.</param>
    /// <remarks>
    /// If <paramref name="ParentClass"/> is 0, then use <paramref name="ParentToken"/>.
    /// </remarks>
    [DebuggerDisplay("{Name}")]
    internal sealed record class FunctionData(string Name, ulong ParentClass, uint ParentToken, ulong ModuleId, ulong[] TypeArgs, ulong[] ParameterTypes);

    /// <param name="Name">The name of the module.</param>
    [DebuggerDisplay("{Name}")]
    internal sealed record class ModuleData(string Name);

    internal sealed record class ModuleScopedToken(ulong ModuleId, uint Token);
}
