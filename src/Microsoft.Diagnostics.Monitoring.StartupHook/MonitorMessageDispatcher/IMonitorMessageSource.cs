// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MonitorMessageArgs : EventArgs
    {
        public MonitorMessageArgs(ProfilerPayloadType payloadType, ProfilerMessageType messageType, IntPtr nativeBuffer, long bufferSize)
        {
            PayloadType = payloadType;
            MessageType = messageType;
            NativeBuffer = nativeBuffer;
            BufferSize = bufferSize;
        }

        public ProfilerPayloadType PayloadType { get; private set; }
        public ProfilerMessageType MessageType { get; private set; }
        public IntPtr NativeBuffer { get; private set; }
        public long BufferSize { get; private set; }
    }

    internal interface IMonitorMessageSource : IDisposable
    {
        public delegate void MonitorMessageHandler(object sender, MonitorMessageArgs args);
        public event MonitorMessageHandler MonitorMessageEvent;
    }
}
