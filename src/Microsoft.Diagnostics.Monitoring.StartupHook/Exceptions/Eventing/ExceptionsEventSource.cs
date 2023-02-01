// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    [EventSource(Name = SourceName)]
    internal sealed class ExceptionsEventSource : EventSource
    {
        public const int ExceptionIdEventId = 1;
        public const int ExceptionEventId = 2;
        public const int ClassDescriptionEventId = 3;
        public const int FunctionDescriptionEventId = 4;
        public const int ModuleDescriptionEventId = 5;
        public const int TokenDescriptionEventId = 6;

        public const string SourceName = "Microsoft.Diagnostics.Monitoring.Exceptions";

        [Event(ExceptionIdEventId)]
        public void WriteExceptionId(
            ulong ExceptionId,
            ulong ExceptionClassId,
            ulong ThrowingMethodId,
            int ILOffset)
        {
            Span<EventData> data = stackalloc EventData[4];

            SetValue(ref data[ExceptionEvents.ExceptionIdPayloads.ExceptionId], ExceptionId);
            SetValue(ref data[ExceptionEvents.ExceptionIdPayloads.ExceptionClassId], ExceptionClassId);
            SetValue(ref data[ExceptionEvents.ExceptionIdPayloads.ThrowingMethodId], ThrowingMethodId);
            SetValue(ref data[ExceptionEvents.ExceptionIdPayloads.ILOffset], ILOffset);

            WriteEventCore(ExceptionIdEventId, data);
        }

        [Event(ExceptionEventId)]
        public void WriteException(
            ulong ExceptionId,
            string? ExceptionMessage)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData namePinned = PinnedData.Create(ExceptionMessage);

            SetValue(ref data[ExceptionEvents.ExceptionPayloads.ExceptionId], ExceptionId);
            SetValue(ref data[ExceptionEvents.ExceptionPayloads.ExceptionMessage], namePinned);

            WriteEventCore(ExceptionEventId, data);
        }

        [Event(ClassDescriptionEventId)]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method required for event source manifest generation.")]
        private unsafe void WriteClassDescription(
            ulong ClassId,
            ulong ModuleId,
            uint Token,
            uint Flags,
            byte* TypeArgs)
        {
        }

        [NonEvent]
        public void WriteClassDescription(
            ulong ClassId,
            ulong ModuleId,
            uint Token,
            uint Flags,
            ulong[] TypeArgs)
        {
            Span<EventData> data = stackalloc EventData[5];
            using PinnedData typeArgsPinned = PinnedData.Create(TypeArgs);

            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.ClassId], ClassId);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.Token], Token);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.Flags], Flags);
            SetValue(ref data[NameIdentificationEvents.ClassDescPayloads.TypeArgs], typeArgsPinned);

            WriteEventCore(ClassDescriptionEventId, data);
        }

        [Event(FunctionDescriptionEventId)]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method required for event source manifest generation.")]
        private unsafe void WriteFunctionDescription(
            ulong FunctionId,
            ulong ClassId,
            uint ClassToken,
            ulong ModuleId,
            string Name,
            byte* TypeArgs)
        {
        }


        [NonEvent]
        public void WriteFunctionDescription(
            ulong FunctionId,
            ulong ClassId,
            uint ClassToken,
            ulong ModuleId,
            string Name,
            ulong[] TypeArgs)
        {
            Span<EventData> data = stackalloc EventData[6];
            using PinnedData namePinned = PinnedData.Create(Name);
            using PinnedData typeArgsPinned = PinnedData.Create(TypeArgs);

            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.FunctionId], FunctionId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassId], ClassId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ClassToken], ClassToken);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.Name], namePinned);
            SetValue(ref data[NameIdentificationEvents.FunctionDescPayloads.TypeArgs], typeArgsPinned);

            WriteEventCore(FunctionDescriptionEventId, data);
        }

        [Event(ModuleDescriptionEventId)]
        public void WriteModuleDescription(
            ulong ModuleId,
            string Name)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData namePinned = PinnedData.Create(Name);

            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.ModuleDescPayloads.Name], namePinned);

            WriteEventCore(ModuleDescriptionEventId, data);
        }

        [Event(TokenDescriptionEventId)]
        public void WriteTokenDescription(
            ulong ModuleId,
            uint Token,
            uint OuterToken,
            string Name)
        {
            Span<EventData> data = stackalloc EventData[4];
            using PinnedData namePinned = PinnedData.Create(Name);

            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.ModuleId], ModuleId);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Token], Token);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.OuterToken], OuterToken);
            SetValue(ref data[NameIdentificationEvents.TokenDescPayloads.Name], namePinned);

            WriteEventCore(TokenDescriptionEventId, data);
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
        private static void SetValue(ref EventData data, in PinnedData value)
        {
            data.DataPointer = value.Address;
            data.Size = value.Size;
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

            public static PinnedData Create<T>(in T[] value) where T : unmanaged
            {
                return new PinnedData(value, Unsafe.SizeOf<T>() * value.Length);
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
