// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    internal sealed class TestExceptionGroupIdentifierCacheCallback :
        ExceptionGroupIdentifierCacheCallback
    {
        public readonly NameCache NameCache = new();

        public readonly Dictionary<ulong, ExceptionGroupData> ExceptionGroupMap = new();

        public readonly Dictionary<ulong, StackFrameData> StackFrameData = new();

        public override void OnClassData(ulong classId, ClassData data)
        {
            Assert.True(NameCache.ClassData.TryAdd(classId, data));
        }

        public override void OnExceptionGroupData(ulong registrationId, ExceptionGroupData data)
        {
            Assert.True(ExceptionGroupMap.TryAdd(registrationId, data));
        }

        public override void OnFunctionData(ulong functionId, FunctionData data)
        {
            Assert.True(NameCache.FunctionData.TryAdd(functionId, data));
        }

        public override void OnModuleData(ulong moduleId, ModuleData data)
        {
            Assert.True(NameCache.ModuleData.TryAdd(moduleId, data));
        }

        public override void OnStackFrameData(ulong frameId, StackFrameData data)
        {
            Assert.True(StackFrameData.TryAdd(frameId, data));
        }

        public override void OnTokenData(ulong moduleId, uint typeToken, TokenData data)
        {
            Assert.True(NameCache.TokenData.TryAdd(new ModuleScopedToken(moduleId, typeToken), data));
        }
    }
}
