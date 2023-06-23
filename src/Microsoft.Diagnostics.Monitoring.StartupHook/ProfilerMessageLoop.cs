// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{

    internal struct DispatchRecord
    {
        public Type PayloadType { get; set; }
        public Action<object> Callback { get; set; }
    }

    internal sealed class ProfilerMessageLoop : IDisposable
    {
        private ManualResetEvent _stopEvent = new(initialState: false);
        Thread? _messagePumpThread;
        private string? _profilerModulePath;

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void GetProfilerMessage(ref RawProfilerMessage message);

        private ConcurrentDictionary<ProfilerCommand, DispatchRecord> _dispatchTable = new();

        public ProfilerMessageLoop()
        {
            _profilerModulePath = Environment.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ModulePath);
            if (!File.Exists(_profilerModulePath))
            {
                throw new FileNotFoundException(_profilerModulePath);
            }

            NativeLibrary.SetDllImportResolver(typeof(ProfilerMessageLoop).Assembly, ResolveDllImport);

        }

        public void Start()
        {
            _messagePumpThread = new Thread(MessageLoop);
        }

        public void RegisterCallback<T>(ProfilerCommand command, Action<T> callback) where T : class
        {
            DispatchRecord dispatch = new()
            {
                PayloadType = typeof(T),
                Callback = (Action<object>)callback
            };
            _dispatchTable[command] = dispatch;
        }

        public void UnregisterCallback(ProfilerCommand command)
        {
            _dispatchTable.TryRemove(command, out _);
        }

        private IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // DllImport for Windows automatically loads in-memory modules (such as the profiler). This is not the case for Linux/MacOS.
            // If we fail resolving the DllImport, we have to load the profiler ourselves.
            if (_profilerModulePath == null ||
                libraryName != ProfilerIdentifiers.LibraryRootFileName)
            {
                return IntPtr.Zero;
            }

            if (NativeLibrary.TryLoad(_profilerModulePath, out IntPtr handle))
            {
                return handle;
            }

            return IntPtr.Zero;
        }

        private void MessageLoop()
        {
            while (true)
            {
                try
                {
                    RawProfilerMessage message = new();
                    GetProfilerMessage(ref message);
                    DispatchMessage(ref message);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private void DispatchMessage(ref RawProfilerMessage message)
        {
            if (message.MessageType != ProfilerMessageType.JsonCommand)
            {
                throw new NotImplementedException($"Message type {message.MessageType} is not supported");
            }

            // For understood command, deserialize the payload into a well-known type
            // and dispatch it for processing on a seperate thread.

            if (_dispatchTable.TryGetValue(message.Command, out DispatchRecord dispatcher))
            {
                var payload = JsonSerializer.Deserialize(message.Payload, dispatcher.PayloadType);
                if (payload != null && dispatcher.Callback != null)
                {
                    try
                    {
                        dispatcher.Callback(payload);
                    }
                    catch
                    {

                    }
                }
            }


            switch (message.Command)
            {
                case ProfilerCommand.CaptureParameters:

                    // 
                    break;

                default:
                    throw new NotImplementedException($"Command {message.Command} is not supported");
            }
        }

        public void Dispose()
        {
            _stopEvent.Set();
            _messagePumpThread?.Join();
            _stopEvent.Dispose();
        }
    }
}
