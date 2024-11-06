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
        private readonly Dictionary<ulong, ExceptionGroup> _exceptionGroupMap = new();
        private readonly NameCache _nameCache = new();
        private readonly Dictionary<ulong, StackFrameInstance> _stackFrames = new();

        public NameCache NameCache => _nameCache;

        public void AddClass(ulong id, uint token, ulong moduleId, ClassFlags flags, ulong[] typeArgs, bool stackTraceHidden)
        {
            _nameCache.ClassData.TryAdd(id, new ClassData(token, moduleId, flags, typeArgs, stackTraceHidden));
        }

        public void AddExceptionGroup(ulong id, ulong exceptionClassId, ulong throwingMethodId, int ilOffset)
        {
            _exceptionGroupMap.Add(id, new ExceptionGroup(exceptionClassId, throwingMethodId, ilOffset));
        }

        public void AddFunction(ulong id, uint methodToken, ulong classId, uint classToken, ulong moduleId, string name, ulong[] typeArgs, ulong[] parameterTypes, bool stackTraceHidden)
        {
            _nameCache.FunctionData.TryAdd(id, new FunctionData(name, methodToken, classId, classToken, moduleId, typeArgs, parameterTypes, stackTraceHidden));
        }

        public void AddStackFrame(ulong id, ulong functionId, int ilOffset)
        {
            _stackFrames.Add(id, new StackFrameInstance(functionId, ilOffset));
        }

        public void AddModule(ulong id, Guid moduleVersionId, string moduleName)
        {
            _nameCache.ModuleData.TryAdd(id, new ModuleData(moduleName, moduleVersionId));
        }

        public void AddToken(ulong moduleId, uint token, uint outerToken, string name, string @namespace, bool stackTraceHidden)
        {
            _nameCache.TokenData.TryAdd(
                new ModuleScopedToken(moduleId, token),
                new TokenData(name, @namespace, outerToken, stackTraceHidden));
        }

        public bool TryGetExceptionGroup(ulong groupId, out ulong exceptionClassId, out ulong throwingMethodId, out int ilOffset)
        {
            exceptionClassId = 0;
            throwingMethodId = 0;
            ilOffset = 0;

            if (!_exceptionGroupMap.TryGetValue(groupId, out ExceptionGroup? identifier))
                return false;

            exceptionClassId = identifier.ClassId;
            throwingMethodId = identifier.ThrowingMethodId;
            ilOffset = identifier.ILOffset;
            return true;
        }

        public bool TryGetStackFrameIds(ulong stackFrameId, out ulong methodId, out int ilOffset)
        {
            methodId = 0;
            ilOffset = 0;

            if (!_stackFrames.TryGetValue(stackFrameId, out StackFrameInstance? instance))
                return false;

            methodId = instance.MethodId;
            ilOffset = instance.ILOffset;

            return true;
        }

        private sealed record class ExceptionGroup(ulong ClassId, ulong ThrowingMethodId, int ILOffset);

        private sealed record class StackFrameInstance(ulong MethodId, int ILOffset);
    }
}
