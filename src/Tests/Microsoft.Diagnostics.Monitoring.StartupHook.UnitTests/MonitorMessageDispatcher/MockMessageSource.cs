﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MockMessageSource : IMonitorMessageSource
    {
        public event IMonitorMessageSource.MonitorMessageHandler? MonitorMessageEvent;

        private void RaiseMessage(MonitorMessageArgs e)
        {
            MonitorMessageEvent?.Invoke(this, e);
        }

        public void RaiseMessage(IProfilerMessage message)
        {
            unsafe
            {
                fixed (byte* payloadPtr = message.Payload)
                {
                    RaiseMessage(new MonitorMessageArgs(
                        message.PayloadType,
                        message.MessageType,
                        new IntPtr(payloadPtr),
                        message.Parameter));
                }
            }

        }

        public void Dispose()
        {
        }
    }
}
