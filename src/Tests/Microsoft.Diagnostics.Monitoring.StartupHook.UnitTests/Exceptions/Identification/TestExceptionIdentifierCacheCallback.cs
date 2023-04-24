// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    internal sealed class TestExceptionIdentifierCacheCallback :
        ExceptionIdentifierCacheCallback
    {
        public readonly NameCache NameCache = new();

        public readonly Dictionary<ulong, ExceptionIdentifierData> ExceptionIdentifierData = new();

        public readonly Dictionary<ulong, StackFrameData> StackFrameData = new();

        public override void OnClassData(ulong classId, ClassData data)
        {
            Assert.True(NameCache.ClassData.TryAdd(classId, data));
        }

        public override void OnExceptionIdentifier(ulong registrationId, ExceptionIdentifierData data)
        {
            Assert.True(ExceptionIdentifierData.TryAdd(registrationId, data));
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
