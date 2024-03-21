// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher
{
    internal sealed class ProfilerMessageSource : IMonitorMessageSource
    {
        public event EventHandler<MonitorMessageArgs>? MonitorMessage;

        public delegate int ProfilerMessageCallback(ushort commandSet, ushort command, IntPtr nativeBuffer, long bufferSize);

        [DllImport(ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterMonitorMessageCallback(ushort commandSet, IntPtr callback);

        [DllImport(ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void UnregisterMonitorMessageCallback(ushort commandSet);

        private readonly ProfilerMessageCallback _messageCallbackDelegate;

        private long _disposedState;

        public ushort CommandSet { get; }

        public ProfilerMessageSource(CommandSet commandSet)
            : this((ushort)commandSet) { }

        public ProfilerMessageSource(ushort commandSet)
        {
            CommandSet = commandSet;
            ProfilerResolver.InitializeResolver<ProfilerMessageSource>();
            _messageCallbackDelegate = OnProfilerMessage;
            RegisterMonitorMessageCallback(CommandSet, Marshal.GetFunctionPointerForDelegate(_messageCallbackDelegate));
        }

        private void RaiseMonitorMessage(MonitorMessageArgs e)
        {
            MonitorMessage?.Invoke(this, e);
        }

        private int OnProfilerMessage(ushort commandSet, ushort command, IntPtr nativeBuffer, long bufferSize)
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

                RaiseMonitorMessage(new MonitorMessageArgs(commandSet, command, nativeBuffer, bufferSize));
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
                UnregisterMonitorMessageCallback(CommandSet);
            }
            catch
            {

            }
        }
    }
}
