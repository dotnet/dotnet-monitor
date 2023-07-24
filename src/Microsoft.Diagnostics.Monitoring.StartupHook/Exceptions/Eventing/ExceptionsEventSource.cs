// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    // This event source should be optimized for speed as much as possible since it will
    // likely be sending many events every second. Avoid any APIs that use params/varargs
    // style calls and avoid heap allocations as much as possible.
    [EventSource(Name = ExceptionEvents.SourceName)]
    internal sealed class ExceptionsEventSource : EventSource
    {
        // Arrays are expected to have a 16-bit field for the length of the array.
        // The length of the array is the number of elements, not the number of bytes.
        private const int ArrayLengthFieldSize = sizeof(short);

        /// <summary>
        /// Amount of time to wait before sending batches of event source events in order to
        /// avoid real-time buffering issues in the runtime eventing infrastructure and the
        /// trace event library event processor.
        /// </summary>
        /// <remarks>
        /// See: https://github.com/dotnet/runtime/issues/76704
        /// </remarks>
        private static readonly TimeSpan EventSourceBufferAvoidanceTimeout = TimeSpan.FromMilliseconds(200);

        private readonly Timer _flushEventsTimer;

        // NOTE: Arrays with a non-"byte" element type are not supported well by in-proc EventListener
        // when using self-describing event format. This format is used to easily support event pipe listening,
        // which is the primary use of this event source.
        public ExceptionsEventSource()
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
            _flushEventsTimer = new Timer(FlushTimerTick);
        }

        protected override void Dispose(bool disposing)
        {
            _flushEventsTimer.Dispose();
            base.Dispose(disposing);
        }

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

            WriteEventCore(ExceptionEvents.EventIds.ExceptionGroup, data);

            RestartFlushingEventTimer();
        }

        [Event(ExceptionEvents.EventIds.ExceptionInstance)]
        public void ExceptionInstance(
            ulong ExceptionId,
            ulong ExceptionGroupId,
            string? ExceptionMessage,
            ulong[] StackFrameIds,
            DateTime Timestamp,
            ulong[] InnerExceptionIds)
        {
            Span<EventData> data = stackalloc EventData[6];
            using PinnedData namePinned = PinnedData.Create(ExceptionMessage);
            Span<byte> stackFrameIdsSpan = stackalloc byte[GetArrayDataSize(StackFrameIds)];
            FillArrayData(stackFrameIdsSpan, StackFrameIds);
            Span<byte> innerExceptionIdsSpan = stackalloc byte[GetArrayDataSize(InnerExceptionIds)];
            FillArrayData(innerExceptionIdsSpan, InnerExceptionIds);

            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionId], ExceptionId);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionGroupId], ExceptionGroupId);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.ExceptionMessage], namePinned);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.StackFrameIds], stackFrameIdsSpan);
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.Timestamp], Timestamp.ToFileTimeUtc());
            SetValue(ref data[ExceptionEvents.ExceptionInstancePayloads.InnerExceptionIds], innerExceptionIdsSpan);

            WriteEventCore(ExceptionEvents.EventIds.ExceptionInstance, data);

            RestartFlushingEventTimer();
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

            WriteEventCore(ExceptionEvents.EventIds.ClassDescription, data);

            RestartFlushingEventTimer();
        }

        [Event(ExceptionEvents.EventIds.FunctionDescription)]
        public void FunctionDescription(
            ulong FunctionId,
            ulong ClassId,
            uint ClassToken,
            ulong ModuleId,
            string Name,
            ulong[] TypeArgs,
            ulong[] ParameterTypes)
        {
            Span<EventData> data = stackalloc EventData[7];
            using PinnedData namePinned = PinnedData.Create(Name);
            Span<byte> typeArgsSpan = stackalloc byte[GetArrayDataSize(TypeArgs)];
            FillArrayData(typeArgsSpan, TypeArgs);
            Span<byte> parameterTypesSpan = stackalloc byte[GetArrayDataSize(ParameterTypes)];
            FillArrayData(parameterTypesSpan, ParameterTypes);

            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.FunctionId], FunctionId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassId], ClassId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassToken], ClassToken);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.Name], namePinned);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.TypeArgs], typeArgsSpan);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ParameterTypes], parameterTypesSpan);

            WriteEventCore(ExceptionEvents.EventIds.FunctionDescription, data);

            RestartFlushingEventTimer();
        }

        [Event(ExceptionEvents.EventIds.ModuleDescription)]
        public void ModuleDescription(
            ulong ModuleId,
            string Name)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData namePinned = PinnedData.Create(Name);

            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.Name], namePinned);

            WriteEventCore(ExceptionEvents.EventIds.ModuleDescription, data);

            RestartFlushingEventTimer();
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

            WriteEventCore(ExceptionEvents.EventIds.StackFrameDescription, data);

            RestartFlushingEventTimer();
        }

        [Event(ExceptionEvents.EventIds.TokenDescription)]
        public void TokenDescription(
            ulong ModuleId,
            uint Token,
            uint OuterToken,
            string Name,
            string FriendlyName)
        {
            Span<EventData> data = stackalloc EventData[5];
            using PinnedData namePinned = PinnedData.Create(Name);
            using PinnedData friendlyNamePinned = PinnedData.Create(FriendlyName);

            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Token], Token);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.OuterToken], OuterToken);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Name], namePinned);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.FriendlyName], friendlyNamePinned);

            WriteEventCore(ExceptionEvents.EventIds.TokenDescription, data);

            RestartFlushingEventTimer();
        }

        [Event(ExceptionEvents.EventIds.Flush)]
        private void Flush()
        {
            WriteEvent(ExceptionEvents.EventIds.Flush);
        }

        [NonEvent]
        private void FlushTimerTick(object? state)
        {
            Flush();
        }

        [NonEvent]
        private void RestartFlushingEventTimer()
        {
            // This will reset the timer to fire after the specified time period. If the timer is already
            // started, it will be reset. If it already finished, it will be started again.
            _flushEventsTimer.Change(EventSourceBufferAvoidanceTimeout, Timeout.InfiniteTimeSpan);
        }

        [NonEvent]
        private unsafe void WriteEventCore(int eventId, Span<EventData> data)
        {
            fixed (EventData* dataPtr = data)
            {
                WriteEventCore(eventId, data.Length, dataPtr);
            }
        }

        [NonEvent]
        private static unsafe void SetValue<T>(ref EventData data, in T value) where T : unmanaged
        {
            data.DataPointer = (nint)Unsafe.AsPointer(ref Unsafe.AsRef(value));
            data.Size = sizeof(T);
        }

        [NonEvent]
        private static unsafe void SetValue(ref EventData data, in Span<byte> value)
        {
            // It is expected that the Span is a wrapper around an array-like value
            // that is pinned or is unmovable from the perspective of GC (e.g. stackalloc).
            // Otherwise, GC might relocate the value after its address is acquired and
            // potentially cause access violations or misinterpretation of the data.
            data.DataPointer = (nint)Unsafe.AsPointer(ref value.GetPinnableReference());
            data.Size = value.Length;
        }

        [NonEvent]
        private static void SetValue(ref EventData data, in PinnedData value)
        {
            data.DataPointer = value.Address;
            data.Size = value.Size;
        }

        private static int GetArrayDataSize<T>(T[] data) where T : unmanaged
        {
            // Arrays are written with a length prefix + the data as bytes
            return ArrayLengthFieldSize + Unsafe.SizeOf<T>() * data.Length;
        }

        private static unsafe void FillArrayData<T>(Span<byte> target, T[] source) where T : unmanaged
        {
#if DEBUG
            // Double-check that the Span is correctly sized. This shouldn't be encountered
            // at runtime so long as Span is constructed correctly using GetArrayDataSize.
            if (target.Length != GetArrayDataSize(source))
                throw new ArgumentOutOfRangeException(nameof(source));
#endif

            // First, copy the length of the array to the beginning of the data
            short length = checked((short)source.Length);
            ReadOnlySpan<byte> lengthSpan = new(Unsafe.AsPointer(ref Unsafe.AsRef(length)), ArrayLengthFieldSize);
            lengthSpan.CopyTo(target);

            // Second, copy the array data as bytes
            if (source.Length > 0)
            {
                ReadOnlySpan<byte> dataSpan = MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(source)),
                    Unsafe.SizeOf<T>() * source.Length);

                dataSpan.CopyTo(target.Slice(ArrayLengthFieldSize));
            }
        }

        private struct PinnedData : IDisposable
        {
            private readonly GCHandle _handle;

            private PinnedData(scoped in object? value, int Size)
            {
                _handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                this.Size = Size;
            }

            public static PinnedData Create(in string? value)
            {
                if (null == value)
                {
                    return new PinnedData(null, 0);
                }
                return new PinnedData(value, sizeof(char) * (value.Length + 1));
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                }
            }

            public IntPtr Address => _handle.AddrOfPinnedObject();

            public int Size { get; }
        }
    }
}
