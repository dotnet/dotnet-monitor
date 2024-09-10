// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Eventing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    // This event source should be optimized for speed as much as possible since it will
    // likely be sending many events every second. Avoid any APIs that use params/varargs
    // style calls and avoid heap allocations as much as possible.
    [EventSource(Name = ExceptionEvents.SourceName)]
    internal sealed class ExceptionsEventSource : AbstractMonitorEventSource
    {
        [Event(ExceptionEvents.EventIds.ExceptionGroup)]
        public void ExceptionGroup(
            ulong ExceptionGroupId,
            ulong ExceptionClassId,
            ulong ThrowingMethodId,
            int ILOffset)
        {
            Span<EventData> data = stackalloc EventData[4];

            SetValue(ref data[ExceptionEvents.ExceptionGroupPayloads.ExceptionGroupId], ExceptionGroupId);
            SetValue(ref data[ExceptionEvents.ExceptionGroupPayloads.ExceptionClassId], ExceptionClassId);
            SetValue(ref data[ExceptionEvents.ExceptionGroupPayloads.ThrowingMethodId], ThrowingMethodId);
            SetValue(ref data[ExceptionEvents.ExceptionGroupPayloads.ILOffset], ILOffset);

            WriteEventWithFlushing(ExceptionEvents.EventIds.ExceptionGroup, data);
        }

        [Event(ExceptionEvents.EventIds.ExceptionInstance)]
        public void ExceptionInstance(
            ulong ExceptionId,
            ulong ExceptionGroupId,
            string ExceptionMessage,
            ulong[] StackFrameIds,
            DateTime Timestamp,
            ulong[] InnerExceptionIds,
            string ActivityId,
            ActivityIdFormat ActivityIdFormat)
        {
            Span<EventData> data = stackalloc EventData[8];
            using PinnedData namePinned = PinnedData.Create(ExceptionMessage);
            Span<byte> stackFrameIdsSpan = stackalloc byte[GetArrayDataSize(StackFrameIds)];
            FillArrayData(stackFrameIdsSpan, StackFrameIds);
            Span<byte> innerExceptionIdsSpan = stackalloc byte[GetArrayDataSize(InnerExceptionIds)];
            FillArrayData(innerExceptionIdsSpan, InnerExceptionIds);
            using PinnedData activityIdPinned = PinnedData.Create(ActivityId);

            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionId], ExceptionId);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionGroupId], ExceptionGroupId);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionMessage], namePinned);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.StackFrameIds], stackFrameIdsSpan);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.Timestamp], Timestamp.ToFileTimeUtc());
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.InnerExceptionIds], innerExceptionIdsSpan);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ActivityId], activityIdPinned);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ActivityIdFormat], ActivityIdFormat);

            WriteEventWithFlushing(ExceptionEvents.EventIds.ExceptionInstance, data);
        }

        [Event(ExceptionEvents.EventIds.ExceptionInstanceUnhandled)]
        public void ExceptionInstanceUnhandled(
            ulong ExceptionId)
        {
            Span<EventData> data = stackalloc EventData[1];

            SetValue(ref data[ExceptionEvents.ExceptionInstanceUnhandledPayloads.ExceptionId], ExceptionId);

            WriteEventWithFlushing(ExceptionEvents.EventIds.ExceptionInstanceUnhandled, data);
        }

        [Event(ExceptionEvents.EventIds.ClassDescription)]
        public void ClassDescription(
            ulong ClassId,
            ulong ModuleId,
            uint Token,
            uint Flags,
            ulong[] TypeArgs)
        {
            Span<EventData> data = stackalloc EventData[5];
            Span<byte> typeArgsSpan = stackalloc byte[GetArrayDataSize(TypeArgs)];
            FillArrayData(typeArgsSpan, TypeArgs);

            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.ClassId], ClassId);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.Token], Token);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.Flags], Flags);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.TypeArgs], typeArgsSpan);

            WriteEventWithFlushing(ExceptionEvents.EventIds.ClassDescription, data);
        }

        [Event(ExceptionEvents.EventIds.FunctionDescription)]
        public void FunctionDescription(
            ulong FunctionId,
            uint MethodToken,
            ulong ClassId,
            uint ClassToken,
            ulong ModuleId,
            string Name,
            ulong[] TypeArgs,
            ulong[] ParameterTypes)
        {
            Span<EventData> data = stackalloc EventData[8];
            using PinnedData namePinned = PinnedData.Create(Name);
            Span<byte> typeArgsSpan = stackalloc byte[GetArrayDataSize(TypeArgs)];
            FillArrayData(typeArgsSpan, TypeArgs);
            Span<byte> parameterTypesSpan = stackalloc byte[GetArrayDataSize(ParameterTypes)];
            FillArrayData(parameterTypesSpan, ParameterTypes);

            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.FunctionId], FunctionId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.MethodToken], MethodToken);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassId], ClassId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassToken], ClassToken);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.Name], namePinned);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.TypeArgs], typeArgsSpan);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ParameterTypes], parameterTypesSpan);

            WriteEventWithFlushing(ExceptionEvents.EventIds.FunctionDescription, data);
        }

        [Event(ExceptionEvents.EventIds.ModuleDescription)]
        public void ModuleDescription(
            ulong ModuleId,
            Guid ModuleVersionId,
            string Name)
        {
            Span<EventData> data = stackalloc EventData[3];
            using PinnedData namePinned = PinnedData.Create(Name);

            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.ModuleVersionId], ModuleVersionId);
            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.Name], namePinned);

            WriteEventWithFlushing(ExceptionEvents.EventIds.ModuleDescription, data);
        }

        [Event(ExceptionEvents.EventIds.StackFrameDescription)]
        public void StackFrameDescription(
            ulong StackFrameId,
            ulong FunctionId,
            int ILOffset)
        {
            Span<EventData> data = stackalloc EventData[3];

            SetValue(ref data[ExceptionEvents.StackFrameIdentifierPayloads.StackFrameId], StackFrameId);
            SetValue(ref data[ExceptionEvents.StackFrameIdentifierPayloads.FunctionId], FunctionId);
            SetValue(ref data[ExceptionEvents.StackFrameIdentifierPayloads.ILOffset], ILOffset);

            WriteEventWithFlushing(ExceptionEvents.EventIds.StackFrameDescription, data);
        }

        [Event(ExceptionEvents.EventIds.TokenDescription)]
        public void TokenDescription(
            ulong ModuleId,
            uint Token,
            uint OuterToken,
            string Name,
            string Namespace)
        {
            Span<EventData> data = stackalloc EventData[5];
            using PinnedData namePinned = PinnedData.Create(Name);
            using PinnedData namespacePinned = PinnedData.Create(Namespace);

            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Token], Token);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.OuterToken], OuterToken);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Name], namePinned);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Namespace], namespacePinned);

            WriteEventWithFlushing(ExceptionEvents.EventIds.TokenDescription, data);
        }

        [Event(ExceptionEvents.EventIds.Flush)]
        protected override void Flush()
        {
            WriteEvent(ExceptionEvents.EventIds.Flush);
        }
    }
}
