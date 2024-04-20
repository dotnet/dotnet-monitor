// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Diagnostics.Monitoring.StartupHook.Monitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MonitorMessageDispatcher : IDisposable
    {
        internal struct MessageDispatchEntry
        {
            public Type DeserializeType { get; set; }
            public Action<object> Callback { get; set; }
        }

        private readonly object _dispatchTableLocker = new();
        private readonly Dictionary<ushort, MessageDispatchEntry> _dispatchTable = new();

        private readonly IMonitorMessageSource _messageSource;

        private long _disposedState;

        public MonitorMessageDispatcher(IMonitorMessageSource messageSource)
        {
            _messageSource = messageSource;
            _messageSource.MonitorMessage += OnMessage;
        }

        public void RegisterCallback<T>(StartupHookCommand command, Action<T> callback)
            => RegisterCallback((ushort)command, callback);

        public void RegisterCallback<T>(ushort command, Action<T> callback)
        {
            MessageDispatchEntry dispatchEntry = new()
            {
                DeserializeType = typeof(T),
                Callback = (obj) =>
                {
                    callback((T)obj);
                }
            };

            lock (_dispatchTableLocker)
            {
                if (!_dispatchTable.TryAdd(command, dispatchEntry))
                {
                    throw new InvalidOperationException("Callback already registered for the requested command.");
                }
            }
        }

        public void UnregisterCallback(StartupHookCommand command)
            => UnregisterCallback((ushort)command);

        public void UnregisterCallback(ushort command)
        {
            lock (_dispatchTableLocker)
            {
                _dispatchTable.Remove(command, out _);
            }
        }

        private void OnMessage(object? sender, MonitorMessageArgs args)
        {
            lock (_dispatchTableLocker)
            {
                if (!_dispatchTable.TryGetValue(args.Command, out MessageDispatchEntry dispatchEntry))
                {
                    throw new NotSupportedException("Unsupported message type.");
                }

                object? payload = null;
                unsafe
                {
                    using UnmanagedMemoryStream memoryStream = new((byte*)args.NativeBuffer.ToPointer(), args.BufferSize);
                    // Exceptions thrown during deserialization will be handled by the message source
                    payload = JsonSerializer.Deserialize(memoryStream, dispatchEntry.DeserializeType);
                }

                if (payload == null)
                {
                    throw new ArgumentException("Could not deserialize.");
                }

                dispatchEntry.Callback(payload);
            }
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _messageSource.MonitorMessage -= OnMessage;
            _messageSource.Dispose();
        }
    }
}
