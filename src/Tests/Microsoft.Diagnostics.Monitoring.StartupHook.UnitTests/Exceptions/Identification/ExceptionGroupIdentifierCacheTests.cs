// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    public sealed class ExceptionGroupIdentifierCacheTests
    {
        private const ulong InvalidId = 0;
        private const uint InvalidToken = 0;

        private static readonly string ThisModuleName =
            Assembly.GetExecutingAssembly().ManifestModule.Name;

        [Fact]
        public void ExceptionGroupIdentifierCache_CallbacksNotRequired()
        {
            Exception ex = new Exception();
            ExceptionGroupIdentifier exceptionGroupId = new(ex);

            List<ExceptionGroupIdentifierCacheCallback> callbacks = new();
            ExceptionGroupIdentifierCache cache = new(callbacks);
            ulong groupId = cache.GetOrAdd(exceptionGroupId);

            Assert.NotEqual(InvalidId, groupId);
        }

        [Fact]
        public void ExceptionGroupIdentifierCache_NotThrownException()
        {
            Exception ex = new Exception();
            ExceptionGroupIdentifier exceptionGroupId = new(ex);

            TestExceptionGroupIdentifierCacheCallback callback = new();

            List<ExceptionGroupIdentifierCacheCallback> callbacks = new()
            {
                callback
            };
            ExceptionGroupIdentifierCache cache = new(callbacks);
            ulong groupId = cache.GetOrAdd(exceptionGroupId);
            Assert.NotEqual(InvalidId, groupId);

            // Validate exception type in cache
            (ulong classId, ClassData classData) = Assert.Single(callback.NameCache.ClassData);
            Assert.NotEqual(InvalidId, classId);
            Assert.NotNull(classData);
            Assert.Equal(ClassFlags.None, classData.Flags);
            Assert.NotEqual(InvalidId, classData.ModuleId);
            Assert.NotEqual(InvalidToken, classData.Token);
            Assert.NotNull(classData.TypeArgs);
            Assert.Empty(classData.TypeArgs);

            // Validate no function data (since exception was not thrown)
            Assert.Empty(callback.NameCache.FunctionData);

            // Validate exception type module in cache
            (ulong moduleId, ModuleData moduleData) = Assert.Single(callback.NameCache.ModuleData);
            Assert.Equal(classData.ModuleId, moduleId);
            Assert.NotNull(moduleData);
            Assert.NotNull(moduleData.Name);
            Assert.Equal(ex.GetType().Module.Name, moduleData.Name);

            // Validate exception type token in cache
            (ModuleScopedToken scopedToken, TokenData data) = Assert.Single(callback.NameCache.TokenData);
            Assert.Equal(moduleId, scopedToken.ModuleId);
            Assert.Equal(classData.Token, scopedToken.Token);
            Assert.Equal(ex.GetType().Name, data.Name);
            Assert.Equal(ex.GetType().Namespace, data.Namespace);
            Assert.Equal(InvalidId, data.OuterToken);

            // Validate exception ID registration
            (ulong groupIdFromCallback, ExceptionGroupData exceptionGroupData) = Assert.Single(callback.ExceptionGroupMap);
            Assert.Equal(groupId, groupIdFromCallback);
            Assert.NotNull(exceptionGroupData);
            Assert.Equal(classId, exceptionGroupData.ExceptionClassId);
            Assert.Equal(InvalidId, exceptionGroupData.ThrowingMethodId);

            // Validate stack frame data (expect none since exception was not thrown)
            StackTrace stackTrace = new(ex, fNeedFileInfo: false);
            ulong[] frameIds = cache.GetOrAdd(stackTrace.GetFrames());
            Assert.Empty(frameIds);
        }

        [Fact]
        public void ExceptionGroupIdentifierCache_ThrownException()
        {
            Exception ex = new ExceptionWithGenericArgs<int>();
            ExceptionGroupIdentifier exceptionGroupId;
            try
            {
                throw ex;
            }
            catch (Exception caught)
            {
                exceptionGroupId = new(caught);
            }

            TestExceptionGroupIdentifierCacheCallback callback = new();

            List<ExceptionGroupIdentifierCacheCallback> callbacks = new()
            {
                callback
            };
            ExceptionGroupIdentifierCache cache = new(callbacks);
            ulong groupId = cache.GetOrAdd(exceptionGroupId);
            Assert.NotEqual(InvalidId, groupId);

            // Validate cache counts
            Assert.Equal(3, callback.NameCache.ClassData.Count);
            Assert.Single(callback.NameCache.FunctionData);
            Assert.Equal(2, callback.NameCache.ModuleData.Count);
            Assert.Equal(3, callback.NameCache.TokenData.Count);

            // Validate exception ID registration
            (ulong groupIdFromCallback, ExceptionGroupData exceptionGroupData) = Assert.Single(callback.ExceptionGroupMap);
            Assert.Equal(groupId, groupIdFromCallback);
            Assert.NotNull(exceptionGroupData);

            // Validate exception type in cache
            Assert.NotEqual(InvalidId, exceptionGroupData.ExceptionClassId);
            Assert.True(callback.NameCache.ClassData.TryGetValue(exceptionGroupData.ExceptionClassId, out ClassData? exceptionClassData));
            Assert.NotNull(exceptionClassData);

            // Validate exception type module in cache
            Assert.NotEqual(InvalidId, exceptionClassData.ModuleId);
            Assert.True(callback.NameCache.ModuleData.TryGetValue(exceptionClassData.ModuleId, out ModuleData? exceptionClassModuleData));
            Assert.NotNull(exceptionClassModuleData);
            Assert.Equal(ex.GetType().Module.Name, exceptionClassModuleData.Name);

            // Validate exception type token in cache
            Assert.NotEqual(InvalidId, exceptionClassData.Token);
            uint parentClassToken = ValidateTokenAndGetOuterToken(
                callback.NameCache,
                exceptionClassData.ModuleId,
                exceptionClassData.Token,
                ex.GetType());

            // Validate exception type parent token in cache
            Assert.NotEqual(InvalidId, parentClassToken);
            parentClassToken = ValidateTokenAndGetOuterToken(
                callback.NameCache,
                exceptionClassData.ModuleId,
                parentClassToken,
                ex.GetType().DeclaringType!);
            Assert.Equal(InvalidId, parentClassToken);

            // Validate exception type generic argument in cache
            ulong genericArgClassId = Assert.Single(exceptionClassData.TypeArgs);
            Assert.True(callback.NameCache.ClassData.TryGetValue(genericArgClassId, out ClassData? genericArgClassData));
            Assert.NotNull(genericArgClassData);

            // Validate exception type generic argument token in cache
            Assert.NotEqual(InvalidId, genericArgClassData.Token);
            parentClassToken = ValidateTokenAndGetOuterToken(
                callback.NameCache,
                genericArgClassData.ModuleId,
                genericArgClassData.Token,
                typeof(int));
            Assert.Equal(InvalidId, parentClassToken);

            // Validate throwing method in cache
            Assert.NotEqual(InvalidId, exceptionGroupData.ThrowingMethodId);
            Assert.True(callback.NameCache.FunctionData.TryGetValue(exceptionGroupData.ThrowingMethodId, out FunctionData? throwingMethodData));
            Assert.NotNull(throwingMethodData);

            // Validate throwing method module in cache
            Assert.NotEqual(InvalidId, throwingMethodData.ModuleId);
            Assert.True(callback.NameCache.ModuleData.TryGetValue(throwingMethodData.ModuleId, out ModuleData? throwingMethodModuleData));
            Assert.NotNull(throwingMethodModuleData);
            Assert.Equal(ThisModuleName, throwingMethodModuleData.Name);

            // Validate throwing method remaining properties
            Assert.Equal(nameof(ExceptionGroupIdentifierCache_ThrownException), throwingMethodData.Name);
            Assert.NotEqual(InvalidId, throwingMethodData.ParentClass);
            Assert.NotEqual(InvalidToken, throwingMethodData.ParentClassToken);
            Assert.Empty(throwingMethodData.TypeArgs);

            // Validate stack frame data
            StackTrace stackTrace = new(ex, fNeedFileInfo: false);
            ulong[] frameIds = cache.GetOrAdd(stackTrace.GetFrames());
            Assert.Equal(stackTrace.FrameCount, frameIds.Length);
            for (int i = 0; i < frameIds.Length; i++)
            {
                ulong frameId = frameIds[i];

                Assert.True(callback.StackFrameData.TryGetValue(frameId, out StackFrameData? frameData));
                Assert.NotEqual(InvalidId, frameData.MethodId);

                Assert.True(callback.NameCache.FunctionData.TryGetValue(frameData.MethodId, out FunctionData? frameMethodData));

                StackFrame? frame = stackTrace.GetFrame(i);
                Assert.NotNull(frame);
                MethodBase? method = frame.GetMethod();
                Assert.NotNull(method);

                Assert.Equal(method.Name, frameMethodData.Name);
            }
        }

        private static uint ValidateTokenAndGetOuterToken(NameCache cache, ulong moduleId, uint token, Type expectedType)
        {
            Assert.True(cache.TokenData.TryGetValue(new ModuleScopedToken(moduleId, token), out TokenData? tokenData));
            Assert.NotNull(tokenData);
            string? expectedTypeName = expectedType.Name;
            string? expectedNamespace = (null == expectedType.DeclaringType) ? expectedType.Namespace : string.Empty;
            Assert.Equal(expectedTypeName, tokenData.Name);
            Assert.Equal(expectedNamespace, tokenData.Namespace);
            return tokenData.OuterToken;
        }

        private sealed class ExceptionWithGenericArgs<T> : Exception
        {
        }
    }
}
