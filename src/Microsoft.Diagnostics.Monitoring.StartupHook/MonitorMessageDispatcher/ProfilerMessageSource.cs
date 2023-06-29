﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class ProfilerMessageSource : IMonitorMessageSource
    {
        public event IMonitorMessageSource.MonitorMessageHandler? MonitorMessageEvent;

        public delegate int ProfilerMessageCallback(IpcCommand command, IntPtr nativeBuffer, long bufferSize);

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterMonitorMessageCallback(ProfilerMessageCallback callback);

        private static ProfilerMessageSource? s_instance;

        public ProfilerMessageSource()
        {
            ProfilerResolver.InitializeResolver<ProfilerMessageSource>();
            RegisterMonitorMessageCallback(OnProfilerMessage);
            s_instance = this;
        }

        private void RaiseMonitorMessage(MonitorMessageArgs e)
        {
            MonitorMessageEvent?.Invoke(this, e);
        }

        private static int OnProfilerMessage(IpcCommand command, IntPtr nativeBuffer, long bufferSize)
        {
            try
            {
                if (bufferSize == 0)
                {
                    throw new ArgumentException(nameof(bufferSize));
                }

                if (nativeBuffer == IntPtr.Zero)
                {
                    throw new ArgumentException(nameof(nativeBuffer));
                }

                ProfilerMessageSource instance = s_instance ?? throw new NotSupportedException();
                instance.RaiseMonitorMessage(new MonitorMessageArgs(command, nativeBuffer, bufferSize));
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }

            return 0;
        }

        public void Dispose()
        {
            s_instance = null;
        }
    }
}
