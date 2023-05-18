// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionsNameCache
    {
        public bool TryGetExceptionId(ulong exceptionId, out ulong exceptionClassId, out ulong throwingMethodId, out int ilOffset);

        //public bool TryGetFunctionId(ulong functionId, out string name, out ulong classId, out uint classToken, out ulong moduleId, out ulong[] typeArgs);

        public bool TryGetStackFrameIds(ulong stackFrameId, out ulong methodId, out int ilOffset);
        public bool TryGetFunctionId(ulong methodId, out FunctionData data);
        public bool TryGetClassId(ulong classId, out ClassData data);
        public bool TryGetModuleId(ulong moduleId, out ModuleData data);
        public bool TryGetToken(ModuleScopedToken moduleScopedToken, out TokenData data);

        NameCache NameCache { get; }
    }
}
