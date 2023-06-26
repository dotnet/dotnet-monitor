// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class ProfilerMessageDispatcher : IDisposable
    {
        internal struct MessageDispatchEntry
        {
            public Type DeserializeType { get; set; }
            public Action<object> Callback { get; set; }
        }

        private static ConcurrentDictionary<ProfilerMessageType, MessageDispatchEntry> s_dispatchTable = new();

        private string? _profilerModulePath;

        public delegate int ProfilerMessageCallback(ProfilerPayloadType payloadType, ProfilerMessageType messageType, IntPtr nativeBuffer, long bufferSize);

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterProfilerMessageCallback(ProfilerMessageCallback callback);


        public ProfilerMessageDispatcher()
        {
            _profilerModulePath = Environment.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ModulePath);
            if (!File.Exists(_profilerModulePath))
            {
                throw new FileNotFoundException(_profilerModulePath);
            }

            NativeLibrary.SetDllImportResolver(typeof(ProfilerMessageDispatcher).Assembly, ResolveDllImport);

        }

#pragma warning disable CA1822 // Mark members as static
        public void Register()
#pragma warning restore CA1822 // Mark members as static
        {
            try
            {
                RegisterProfilerMessageCallback(OnProfilerMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public void RegisterCallback<T>(ProfilerMessageType messageType, Action<T> callback) where T : class
#pragma warning restore CA1822 // Mark members as static
        {
            MessageDispatchEntry dispatchEntry = new()
            {
                DeserializeType = typeof(T),
                Callback = (obj) => 
                {
                    T tObj = (T)obj ?? throw new InvalidOperationException();
                    callback(tObj);
                }
            };

            if (!s_dispatchTable.TryAdd(messageType, dispatchEntry))
            {
                throw new InvalidOperationException($"Callback for message {messageType} already registered");
            }
        }


#pragma warning disable CA1822 // Mark members as static
        public void UnregisterCallback(ProfilerMessageType messageType)
        {
#pragma warning restore CA1822 // Mark members as static
            s_dispatchTable.TryRemove(messageType, out _);
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

        private static int OnProfilerMessage(ProfilerPayloadType payloadType, ProfilerMessageType messageType, IntPtr nativeBuffer, long bufferSize)
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

                if (payloadType != ProfilerPayloadType.Utf8Json)
                {
                    throw new NotSupportedException("Unsupported payload type");
                }

                object? payload = null;
                if (!s_dispatchTable.TryGetValue(messageType, out MessageDispatchEntry dispatchEntry))
                {
                    throw new NotSupportedException("Unsupported message type");
                }

                unsafe
                {
                    using UnmanagedMemoryStream memoryStream = new((byte*)nativeBuffer.ToPointer(), bufferSize);
                    payload = JsonSerializer.Deserialize(memoryStream, dispatchEntry.DeserializeType);
                }

                if (payload == null)
                {
                    throw new ArgumentException("Invalid ");
                }

                dispatchEntry.Callback(payload);

            }
            catch (Exception ex)
            {
                return Marshal.GetHRForException(ex);
            }

            return 0;
        }

        public void Dispose()
        {
            s_dispatchTable.Clear();
        }
    }
}
