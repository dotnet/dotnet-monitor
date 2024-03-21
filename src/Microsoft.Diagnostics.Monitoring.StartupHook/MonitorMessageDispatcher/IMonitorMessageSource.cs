// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MonitorMessageArgs : EventArgs
    {
        public MonitorMessageArgs(ushort commandSet, ushort command, IntPtr nativeBuffer, long bufferSize)
        {
            CommandSet = commandSet;
            Command = command;
            NativeBuffer = nativeBuffer;
            BufferSize = bufferSize;
        }

        public ushort CommandSet { get; private set; }
        public ushort Command { get; private set; }
        public IntPtr NativeBuffer { get; private set; }
        public long BufferSize { get; private set; }
    }

    internal interface IMonitorMessageSource : IDisposable
    {
        public ushort CommandSet { get; }

        public event EventHandler<MonitorMessageArgs> MonitorMessage;
    }
}
