// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    public sealed class ExceptionsEventSourceTests
    {
        private const string InvalidOperationExceptionMessage = "Operation is not valid due to the current state of the object.";
        private const string ObjectDisposedExceptionMessage = "Cannot access a disposed object.";
        private const string OperationCancelledExceptionMessage = "The operation was canceled.";

        [Theory]
        [InlineData(0, 3, 14, int.MaxValue)]
        [InlineData(1, 0, ulong.MaxValue, 19)]
        [InlineData(7, ulong.MaxValue, 0, 17)]
        [InlineData(ulong.MaxValue, 29, 41, 0)]
        public void ExceptionsEventSource_WriteExceptionId_Event(ulong id, ulong classId, ulong methodId, int ilOffset)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.ExceptionIdentifier(id, classId, methodId, ilOffset);

            (ulong exceptionId, ExceptionIdentifierData data) = Assert.Single(listener.ExceptionIdentifiers);
            Assert.Equal(id, exceptionId);
            Assert.Equal(classId, data.ExceptionClassId);
            Assert.Equal(methodId, data.ThrowingMethodId);
            Assert.Equal(ilOffset, data.ILOffset);
        }

        [Fact]
        public void ExceptionsEventSource_WriteExceptionId_LevelTooHigh()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Warning);

            source.ExceptionIdentifier(1, 2, 3, 4);

            Assert.Empty(listener.ExceptionIdentifiers);
        }

        [Fact]
        public void ExceptionsEventSource_WriteExceptionId_NotEnabled()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();

            source.ExceptionIdentifier(4, 3, 2, 1);

            Assert.Empty(listener.ExceptionIdentifiers);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(1, "")]
        [InlineData(7, InvalidOperationExceptionMessage)]
        [InlineData(ulong.MaxValue - 1, OperationCancelledExceptionMessage)]
        [InlineData(ulong.MaxValue, ObjectDisposedExceptionMessage)]
        public void ExceptionsEventSource_WriteException_Event(ulong id, string message)
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Informational);

            source.ExceptionInstance(id, message);

            ExceptionInstance instance = Assert.Single(listener.Exceptions);
            Assert.Equal(id, instance.ExceptionId);
            Assert.Equal(CoalesceNull(message), instance.ExceptionMessage);
        }

        [Fact]
        public void ExceptionsEventSource_WriteException_LevelTooHigh()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();
            listener.EnableEvents(source, EventLevel.Warning);

            source.ExceptionInstance(7, ObjectDisposedExceptionMessage);

            Assert.Empty(listener.Exceptions);
        }

        [Fact]
        public void ExceptionsEventSource_WriteException_NotEnabled()
        {
            using ExceptionsEventSource source = new();

            using ExceptionsEventListener listener = new();

            source.ExceptionInstance(9, OperationCancelledExceptionMessage);

            Assert.Empty(listener.Exceptions);
        }

        private static string CoalesceNull(string? value)
        {
            return value ?? ExceptionsEventListener.NullValue;
        }
    }
}
