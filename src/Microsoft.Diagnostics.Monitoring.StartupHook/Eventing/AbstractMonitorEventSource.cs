// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Eventing
{
    internal abstract class AbstractMonitorEventSource : EventSource
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

        /// <summary>
        /// Amount of time to wait for the flush timer to finish inflight callbacks.
        /// </summary>
        private static readonly TimeSpan FlushTimerFinishedTimeout = TimeSpan.FromMilliseconds(100);

        private readonly Timer _flushEventsTimer;

        // NOTE: Arrays with a non-"byte" element type are not supported well by in-proc EventListener
        // when using self-describing event format. This format is used to easily support event pipe listening.
        public AbstractMonitorEventSource()
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
            _flushEventsTimer = new Timer(FlushTimerTick);
        }

        protected override void Dispose(bool disposing)
        {
            ManualResetEvent flushTimerFinishedHandle = new(false);

            // Disposing the timer does not wait for any inflight callbacks to finish.
            // Pass a wait handle that the timer will signal once all callbacks have finished
            // and wait on it with a timeout.
            _flushEventsTimer.Dispose(flushTimerFinishedHandle);

            // Intentionally leak wait handle if timeout occurs; the timer will still have a
            // reference to the handle, thus it cannot be disposed until the timer callbacks
            // finish. It is assumed that the timer callback will finish quickly, and thus
            // leaking this handle will be extremely rare.
            if (flushTimerFinishedHandle.WaitOne(FlushTimerFinishedTimeout))
            {
                flushTimerFinishedHandle.Dispose();
            }

            base.Dispose(disposing);
        }

        // Abstract base classes cannot define events so make the derived class define the flush event.
        protected abstract void Flush();


        [NonEvent]
        private void FlushTimerTick(object? state)
        {
            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();
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
        protected unsafe void WriteEventWithFlushing(int eventId)
        {
            WriteEvent(eventId);
            RestartFlushingEventTimer();
        }

        [NonEvent]
        protected unsafe void WriteEventWithFlushing(int eventId, Span<EventData> data)
        {
            fixed (EventData* dataPtr = data)
            {
                WriteEventCore(eventId, data.Length, dataPtr);
            }

            RestartFlushingEventTimer();
        }

        [NonEvent]
        protected static unsafe void SetValue<T>(ref EventData data, in T value) where T : unmanaged
        {
            data.DataPointer = (nint)Unsafe.AsPointer(ref Unsafe.AsRef(value));
            data.Size = sizeof(T);
        }

        [NonEvent]
        protected static unsafe void SetValue(ref EventData data, in Span<byte> value)
        {
            // It is expected that the Span is a wrapper around an array-like value
            // that is pinned or is unmovable from the perspective of GC (e.g. stackalloc).
            // Otherwise, GC might relocate the value after its address is acquired and
            // potentially cause access violations or misinterpretation of the data.
            data.DataPointer = (nint)Unsafe.AsPointer(ref value.GetPinnableReference());
            data.Size = value.Length;
        }

        [NonEvent]
        protected static void SetValue(ref EventData data, in PinnedData value)
        {
            data.DataPointer = value.Address;
            data.Size = value.Size;
        }

        [NonEvent]
        protected static int GetArrayDataSize<T>(T[] data) where T : unmanaged
        {
            // Arrays are written with a length prefix + the data as bytes
            return ArrayLengthFieldSize + Unsafe.SizeOf<T>() * data.Length;
        }

        [NonEvent]
        protected static unsafe void FillArrayData<T>(Span<byte> target, T[] source) where T : unmanaged
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

        protected struct PinnedData : IDisposable
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
