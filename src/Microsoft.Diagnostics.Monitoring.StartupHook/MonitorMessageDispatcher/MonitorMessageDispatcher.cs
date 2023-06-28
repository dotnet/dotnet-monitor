// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.IO;
using System;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MonitorMessageDispatcher : IDisposable
    {
        internal struct MessageDispatchEntry
        {
            public Type DeserializeType { get; set; }
            public Action<object> Callback { get; set; }
        }

        private ConcurrentDictionary<ProfilerMessageType, MessageDispatchEntry> _dispatchTable = new();

        private IMonitorMessageSource _messageSource;

        private long _disposedState;

        public MonitorMessageDispatcher(IMonitorMessageSource messageSource)
        {
            _messageSource = messageSource;
            _messageSource.MonitorMessageEvent += OnMessage;
        }

        public void RegisterCallback<T>(ProfilerMessageType messageType, Action<T> callback)
        {
            MessageDispatchEntry dispatchEntry = new()
            {
                DeserializeType = typeof(T),
                Callback = (obj) =>
                {
                    callback((T)obj);
                }
            };

            if (!_dispatchTable.TryAdd(messageType, dispatchEntry))
            {
                throw new InvalidOperationException("Callback already registered for the requested message type.");
            }
        }

        public void UnregisterCallback(ProfilerMessageType messageType)
        {
            _dispatchTable.TryRemove(messageType, out _);
        }

        private void OnMessage(object sender, MonitorMessageArgs args)
        {
            if (args.PayloadType != ProfilerPayloadType.Utf8Json)
            {
                throw new NotSupportedException("Unsupported payload type.");
            }

            object? payload = null;
            if (!_dispatchTable.TryGetValue(args.MessageType, out MessageDispatchEntry dispatchEntry))
            {
                throw new NotSupportedException("Unsupported message type.");
            }

            unsafe
            {
                using UnmanagedMemoryStream memoryStream = new((byte*)args.NativeBuffer.ToPointer(), args.BufferSize);
                payload = JsonSerializer.Deserialize(memoryStream, dispatchEntry.DeserializeType);
            }

            if (payload == null)
            {
                throw new ArgumentException("Could not deserialize.");
            }

            dispatchEntry.Callback(payload);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _messageSource.MonitorMessageEvent -= OnMessage;
            _messageSource.Dispose();
        }
    }
}
