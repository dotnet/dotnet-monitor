﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    internal sealed class ExceptionsEventSourceIdentifierCacheCallback :
        ExceptionGroupIdentifierCacheCallback
    {
        private readonly ExceptionsEventSource _source;

        public ExceptionsEventSourceIdentifierCacheCallback(ExceptionsEventSource source)
        {
            _source = source;
        }

        public override void OnClassData(ulong classId, ClassData data)
        {
            _source.ClassDescription(
                classId,
                data.ModuleId,
                data.Token,
                (uint)data.Flags,
                data.TypeArgs);
        }

        public override void OnExceptionGroupData(ulong groupId, ExceptionGroupData data)
        {
            _source.ExceptionGroup(
                groupId,
                data.ExceptionClassId,
                data.ThrowingMethodId,
                data.ILOffset);
        }

        public override void OnFunctionData(ulong functionId, FunctionData data)
        {
            _source.FunctionDescription(
                functionId,
                data.ParentClass,
                data.ParentToken,
                data.ModuleId,
                data.Name,
                data.TypeArgs,
                data.ParameterTypes);
        }

        public override void OnModuleData(ulong moduleId, ModuleData data)
        {
            _source.ModuleDescription(
                moduleId,
                data.Name);
        }

        public override void OnStackFrameData(ulong frameId, StackFrameData data)
        {
            _source.StackFrameDescription(
                frameId,
                data.MethodId,
                data.ILOffset);
        }

        public override void OnTokenData(ulong moduleId, uint typeToken, TokenData data)
        {
            _source.TokenDescription(
                moduleId,
                typeToken,
                data.OuterToken,
                data.Name);
        }
    }
}
