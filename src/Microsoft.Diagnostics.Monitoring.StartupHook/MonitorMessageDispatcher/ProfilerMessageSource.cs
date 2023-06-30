// Licensed to the .NET Foundation under one or more agreements.
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

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void UnregisterMonitorMessageCallback();
        

        public ProfilerMessageSource()
        {
            ProfilerResolver.InitializeResolver<ProfilerMessageSource>();
            RegisterMonitorMessageCallback(OnProfilerMessage);
        }

        private void RaiseMonitorMessage(MonitorMessageArgs e)
        {
            MonitorMessageEvent?.Invoke(this, e);
        }

        private int OnProfilerMessage(IpcCommand command, IntPtr nativeBuffer, long bufferSize)
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

                RaiseMonitorMessage(new MonitorMessageArgs(command, nativeBuffer, bufferSize));
            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }

            return 0;
        }

        public void Dispose()
        {
            try
            {
                UnregisterMonitorMessageCallback();
            }
            catch
            {

            }
        }
    }
}
