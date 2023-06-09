// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class EventExceptionsPipelineNameCache : IExceptionsNameCache
    {
        private readonly List<ExceptionInstance> _exceptions = new();
        private readonly Dictionary<ulong, ExceptionGroup> _exceptionGroupMap = new();
        private readonly NameCache _nameCache = new();

        public NameCache NameCache => _nameCache;

        public void AddClass(ulong id, uint token, ulong moduleId, ClassFlags flags, ulong[] typeArgs)
        {
            _nameCache.ClassData.TryAdd(id, new ClassData(token, moduleId, flags, typeArgs ?? Array.Empty<ulong>()));
        }

        public void AddExceptionGroup(ulong id, ulong exceptionClassId, ulong throwingMethodId, int ilOffset)
        {
            _exceptionGroupMap.Add(id, new ExceptionGroup(exceptionClassId, throwingMethodId, ilOffset));
        }

        public void AddExceptionInstance(ulong groupId, string message)
        {
            _exceptions.Add(new ExceptionInstance(groupId, message));
        }

        public void AddFunction(ulong id, ulong classId, uint classToken, ulong moduleId, string name, ulong[] typeArgs)
        {
            _nameCache.FunctionData.TryAdd(id, new FunctionData(name, classId, classToken, moduleId, typeArgs ?? Array.Empty<ulong>()));
        }

        public void AddModule(ulong id, string moduleName)
        {
            _nameCache.ModuleData.TryAdd(id, new ModuleData(moduleName));
        }

        public void AddToken(ulong moduleId, uint token, uint outerToken, string name)
        {
            _nameCache.TokenData.TryAdd(
                new ModuleScopedToken(moduleId, token),
                new TokenData(name, outerToken));
        }

        public bool TryGetExceptionGroup(ulong groupId, out ulong exceptionClassId, out ulong throwingMethodId, out int ilOffset)
        {
            exceptionClassId = 0;
            throwingMethodId = 0;
            ilOffset = 0;

            if (!_exceptionGroupMap.TryGetValue(groupId, out ExceptionGroup identifier))
                return false;

            exceptionClassId = identifier.ClassId;
            throwingMethodId = identifier.ThrowingMethodId;
            ilOffset = identifier.ILOffset;
            return true;
        }

        private sealed record class ExceptionGroup(ulong ClassId, ulong ThrowingMethodId, int ILOffset);

        private sealed record class ExceptionInstance(ulong GroupId, string Message);
    }
}
