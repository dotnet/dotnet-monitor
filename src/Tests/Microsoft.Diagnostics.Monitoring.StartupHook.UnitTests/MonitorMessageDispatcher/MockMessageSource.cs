// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class MockMessageSource : IMonitorMessageSource
    {
        public event EventHandler<MonitorMessageArgs>? MonitorMessage;

        private void RaiseMessage(MonitorMessageArgs e)
        {
            MonitorMessage?.Invoke(this, e);
        }

        public void RaiseMessage(IProfilerMessage message)
        {
            unsafe
            {
                fixed (byte* payloadPtr = message.Payload)
                {
                    RaiseMessage(new MonitorMessageArgs(
                        message.Command,
                        new IntPtr(payloadPtr),
                        message.Payload.Length));
                }
            }

        }

        public void Dispose()
        {
        }
    }
}
