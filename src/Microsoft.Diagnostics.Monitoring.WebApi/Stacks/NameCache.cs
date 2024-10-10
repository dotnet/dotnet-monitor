// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// <param name="StackTraceHidden">If the class has <see cref="StackTraceHiddenAttribute"/>.</param>
    /// <remarks>
    /// The name of the class can be retrieved from the corresponding <see cref="TokenData"/>.
    /// </remarks>
    internal sealed record class ClassData(uint Token, ulong ModuleId, ClassFlags Flags, ulong[] TypeArgs, bool StackTraceHidden);

    /// <param name="Name">The name of the token.</param>
    /// <param name="Namespace">The namespace of the Name.</param>
    /// <param name="OuterToken">The metadata token of the parent container.</param>
    /// <param name="StackTraceHidden">If the token has <see cref="StackTraceHiddenAttribute"/>.</param>
    [DebuggerDisplay("{Name}")]
    internal sealed record class TokenData(string Name, string Namespace, uint OuterToken, bool StackTraceHidden);

    /// <param name="Name">The name of the function.</param>
    /// <param name="MethodToken">The method token of the function (methodDef token).</param>
    /// <param name="ParentClass">The parent class identifier of the function.</param>
    /// <param name="ParentClassToken">The parent metadata token of the function (typeDef token).</param>
    /// <param name="ModuleId">The identifier of the module that contains the function.</param>
    /// <param name="TypeArgs">The class identifiers of the generic type arguments of the function.</param>
    /// <param name="ParameterTypes">The class identifiers of the parameter types of the function.</param>
    /// <param name="StackTraceHidden">If the function has <see cref="StackTraceHiddenAttribute"/>.</param>
    /// <remarks>
    /// If <paramref name="ParentClass"/> is 0, then use <paramref name="ParentClassToken"/>.
    /// </remarks>
    [DebuggerDisplay("{Name}")]
    internal sealed record class FunctionData(string Name, uint MethodToken, ulong ParentClass, uint ParentClassToken, ulong ModuleId, ulong[] TypeArgs, ulong[] ParameterTypes, bool StackTraceHidden);

    /// <param name="Name">The name of the module.</param>
    /// <param name="ModuleVersionId">The version identifier of the module.</param>
    [DebuggerDisplay("{Name}")]
    internal sealed record class ModuleData(string Name, Guid ModuleVersionId);

    internal sealed record class ModuleScopedToken(ulong ModuleId, uint Token);
}
