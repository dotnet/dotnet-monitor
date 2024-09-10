// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMoniker.Current)]
    public sealed class ExceptionsEventSourceTests
    {
        private const string InvalidOperationExceptionMessage = "Operation is not valid due to the current state of the object.";
        private const string ObjectDisposedExceptionMessage = "Cannot access a disposed object.";
        private const string OperationCancelledExceptionMessage = "The operation was canceled.";
        private const string NonEmptyGuidString = "00000000-0000-0000-0000-000000000001";

        [Theory]
        [InlineData(0, 3, 14, int.MaxValue)]
        [InlineData(1, 0, ulong.MaxValue, 19)]
        [InlineData(7, ulong.MaxValue, 0, 17)]
        [InlineData(ulong.MaxValue, 29, 41, 0)]
        public void ExceptionsEventSource_WriteExceptionGroup_Event(ulong id, ulong classId, ulong methodId, int ilOffset)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.ExceptionGroup(id, classId, methodId, ilOffset);

            (ulong exceptionId, ExceptionGroupData data) = Assert.Single(listener.ExceptionGroups);
            Assert.Equal(id, exceptionId);
            Assert.Equal(classId, data.ExceptionClassId);
            Assert.Equal(methodId, data.ThrowingMethodId);
            Assert.Equal(ilOffset, data.ILOffset);
        }

        [Fact]
        public void ExceptionsEventSource_WriteExceptionGroup_LevelTooHigh()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Warning);

            source.ExceptionGroup(1, 2, 3, 4);

            Assert.Empty(listener.ExceptionGroups);
        }

        [Fact]
        public void ExceptionsEventSource_WriteExceptionGroup_NotEnabled()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();

            source.ExceptionGroup(4, 3, 2, 1);

            Assert.Empty(listener.ExceptionGroups);
        }

        [Theory]
        [InlineData(0, 0, null, "0,0,0", "", null, ActivityIdFormat.Unknown)]
        [InlineData(1, 5, "", "1,2", "1", NonEmptyGuidString, ActivityIdFormat.Hierarchical)]
        [InlineData(7, 13, InvalidOperationExceptionMessage, "", "3,5", NonEmptyGuidString, ActivityIdFormat.W3C)]
        [InlineData(ulong.MaxValue - 1, ulong.MaxValue - 1, OperationCancelledExceptionMessage, "3,5,7", "2", NonEmptyGuidString, ActivityIdFormat.W3C)]
        [InlineData(ulong.MaxValue, ulong.MaxValue, ObjectDisposedExceptionMessage, "2,7,11", "9,8,4", NonEmptyGuidString, ActivityIdFormat.W3C)]
        public void ExceptionsEventSource_WriteException_Event(ulong id, ulong groupId, string message, string frameIdsString, string innerExceptionIdsString, string activityId, ActivityIdFormat activityIdFormat)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            ulong[] frameIds = frameIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToArray();
            DateTime timestamp = DateTime.UtcNow;
            ulong[] innerExceptionIds = innerExceptionIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToArray();

            source.ExceptionInstance(id, groupId, message, frameIds, timestamp, innerExceptionIds, activityId, activityIdFormat);

            ExceptionInstance instance = Assert.Single(listener.Exceptions);
            Assert.Equal(id, instance.Id);
            Assert.Equal(groupId, instance.GroupId);
            Assert.Equal(CoalesceNull(message), instance.ExceptionMessage);
            // We would normally expect the following to return an array of the stack frame IDs
            // but in-process listener doesn't decode non-byte arrays correctly.
            Assert.Equal(Array.Empty<ulong>(), instance.StackFrameIds);
            Assert.Equal(timestamp, instance.Timestamp);
            // We would normally expect the following to return an array of the inner exception IDs
            // but in-process listener doesn't decode non-byte arrays correctly.
            Assert.Equal(Array.Empty<ulong>(), instance.InnerExceptionIds);

            Assert.Equal(CoalesceNull(activityId), instance.ActivityId);
            Assert.Equal(activityIdFormat, instance.ActivityIdFormat);
        }

        [Fact]
        public void ExceptionsEventSource_WriteException_LevelTooHigh()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Warning);

            source.ExceptionInstance(5, 7, ObjectDisposedExceptionMessage, Array.Empty<ulong>(), DateTime.UtcNow, Array.Empty<ulong>(), string.Empty, ActivityIdFormat.Unknown);

            Assert.Empty(listener.Exceptions);
        }

        [Fact]
        public void ExceptionsEventSource_WriteException_NotEnabled()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();

            source.ExceptionInstance(7, 9, OperationCancelledExceptionMessage, Array.Empty<ulong>(), DateTime.UtcNow, Array.Empty<ulong>(), string.Empty, ActivityIdFormat.Unknown);

            Assert.Empty(listener.Exceptions);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 42, 7)]
        public void ExceptionsEventSource_WriteStackFrame_Event(ulong id, ulong methodId, int ilOffset)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.StackFrameDescription(id, methodId, ilOffset);

            Assert.True(listener.StackFrameIdentifiers.TryGetValue(id, out StackFrameIdentifier? frameIdentifier));
            Assert.Equal(methodId, frameIdentifier.MethodId);
            Assert.Equal(ilOffset, frameIdentifier.ILOffset);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, "", new ulong[0], new ulong[0])]
        [InlineData(1, 100663639, 128, 256, 512, "ThrowObjectDisposedException", new ulong[1] { 1024 }, new ulong[2] { 2048, 4096 })]
        public void ExceptionsEventSource_WriteFunction_Event(
            ulong functionId,
            uint methodToken,
            ulong classId,
            uint classToken,
            ulong moduleId,
            string name,
            ulong[] typeArgs,
            ulong[] parameterTypes)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.FunctionDescription(functionId, methodToken, classId, classToken, moduleId, name, typeArgs, parameterTypes);

            Assert.True(listener.NameCache.FunctionData.TryGetValue(functionId, out FunctionData? function));
            Assert.Equal(methodToken, function.MethodToken);
            Assert.Equal(classId, function.ParentClass);
            Assert.Equal(classToken, function.ParentClassToken);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(name, function.Name);
            // We would normally expect the following to return an array of the stack frame IDs
            // but in-process listener doesn't decode non-byte arrays correctly.
            Assert.Equal(Array.Empty<ulong>(), function.TypeArgs);
            Assert.Equal(Array.Empty<ulong>(), function.ParameterTypes);
        }

        [Theory]
        [InlineData(0, "00000000-0000-0000-0000-000000000000", "")]
        [InlineData(1, NonEmptyGuidString, "Module")]
        public void ExceptionsEventSource_WriteModule_Event(
            ulong moduleId,
            Guid moduleVersionId,
            string name)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.ModuleDescription(moduleId, moduleVersionId, name);

            Assert.True(listener.NameCache.ModuleData.TryGetValue(moduleId, out ModuleData? module));
            Assert.Equal(moduleVersionId, module.ModuleVersionId);
            Assert.Equal(name, module.Name);
        }

        private static string CoalesceNull(string? value)
        {
            return value ?? ExceptionsEventListener.NullValue;
        }
    }
}
