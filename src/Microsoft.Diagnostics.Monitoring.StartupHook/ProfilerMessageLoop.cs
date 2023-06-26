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

    internal struct DispatchRecord
    {
        public Type PayloadType { get; set; }
        public Action<object> Callback { get; set; }
    }

    internal sealed class ProfilerMessageLoop : IDisposable
    {
        private string? _profilerModulePath;

        public delegate void ProfilerMessageCallback(ProfilerMessageType messageType, ProfilerCommand command, long bufferSize, IntPtr buffer);

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterProfilerMessageCallback(ProfilerMessageCallback callback);


        private static ConcurrentDictionary<ProfilerCommand, DispatchRecord> s_dispatchTable = new();

        public ProfilerMessageLoop()
        {
            _profilerModulePath = Environment.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ModulePath);
            if (!File.Exists(_profilerModulePath))
            {
                throw new FileNotFoundException(_profilerModulePath);
            }

            NativeLibrary.SetDllImportResolver(typeof(ProfilerMessageLoop).Assembly, ResolveDllImport);

        }

#pragma warning disable CA1822 // Mark members as static
        public void Register()
#pragma warning restore CA1822 // Mark members as static
        {
            try
            {
                RegisterProfilerMessageCallback(OnProfilerMessage);
                Console.WriteLine("[startup-hook] REGISTERED CALLBACK");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public void RegisterCallback<T>(ProfilerCommand command, Action<T> callback) where T : class
#pragma warning restore CA1822 // Mark members as static
        {
            DispatchRecord dispatch = new()
            {
                PayloadType = typeof(T),
                Callback = (o) => 
                {
                    T converted = (T)o;
                    if (converted != null)
                    {
                        callback(converted);
                    }
                }
            };
            s_dispatchTable[command] = dispatch;
        }


#pragma warning disable CA1822 // Mark members as static
        public void UnregisterCallback(ProfilerCommand command)
        {
#pragma warning restore CA1822 // Mark members as static
            s_dispatchTable.TryRemove(command, out _);
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

        private static void OnProfilerMessage(ProfilerMessageType messageType, ProfilerCommand command, long bufferSize, IntPtr nativeBuffer)
        {
            try
            {
                if (messageType != ProfilerMessageType.JsonCommand)
                {
                    throw new NotImplementedException($"Message type {messageType} is not supported");
                }

                object? payload = null;
                if (!s_dispatchTable.TryGetValue(command, out DispatchRecord dispatcher))
                {
                    throw new NotImplementedException($"Message command {command} is not supported");
                }

                unsafe
                {
                    using UnmanagedMemoryStream memoryStream = new((byte*)nativeBuffer.ToPointer(), bufferSize);
                    payload = JsonSerializer.Deserialize(memoryStream, dispatcher.PayloadType);
                }

                if (payload != null && dispatcher.Callback != null)
                {
                    dispatcher.Callback(payload);
                }
                else
                {
                    throw new NotImplementedException("Failed to deserialized");
                }
            }
            catch (Exception ex)
            {
                // JSFIX: Return hresult
                Console.WriteLine(ex.ToString());
            }

        }

        public void Dispose()
        {
            s_dispatchTable.Clear();
        }
    }
}
