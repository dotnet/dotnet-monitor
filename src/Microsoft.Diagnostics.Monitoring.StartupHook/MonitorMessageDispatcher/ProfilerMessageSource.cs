// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Diagnostics.Monitoring.StartupHook.Monitoring;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class ProfilerMessageSource : IMonitorMessageSource
    {
        public event EventHandler<MonitorMessageArgs>? MonitorMessage;

        public delegate int ProfilerMessageCallback(ushort command, IntPtr nativeBuffer, long bufferSize);

        [DllImport(ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterMonitorMessageCallback(ushort commandSet, IntPtr callback);

        [DllImport(ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void UnregisterMonitorMessageCallback(ushort commandSet);

        private readonly ProfilerMessageCallback _messageCallbackDelegate;

        private readonly ushort _commandSet;

        private long _disposedState;


        public ProfilerMessageSource(CommandSet commandSet)
            : this((ushort)commandSet) { }

        public ProfilerMessageSource(ushort commandSet)
        {
            _commandSet = commandSet;

            ProfilerResolver.InitializeResolver<ProfilerMessageSource>();
            _messageCallbackDelegate = OnProfilerMessage;
            RegisterMonitorMessageCallback(_commandSet, Marshal.GetFunctionPointerForDelegate(_messageCallbackDelegate));
        }

        private void RaiseMonitorMessage(MonitorMessageArgs e)
        {
            MonitorMessage?.Invoke(this, e);
        }

        private int OnProfilerMessage(ushort command, IntPtr nativeBuffer, long bufferSize)
        {
            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();

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
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            try
            {
                UnregisterMonitorMessageCallback(_commandSet);
            }
            catch
            {

            }
        }
    }
}
