// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal static class ProfilerResolver
    {
        private static readonly Lazy<string> s_profilerModulePath = new Lazy<string>(() => Environment.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ModulePath) ?? string.Empty);
        private static readonly Lazy<bool> s_profilerModuleExists = new Lazy<bool>(() => File.Exists(s_profilerModulePath.Value));

        private static readonly HashSet<Assembly> s_registeredAssemblies = new HashSet<Assembly>();
        private static readonly object s_registeredAssembliesLocker = new();

        public static void InitializeResolver(Type type)
        {
            if (!s_profilerModuleExists.Value)
            {
                throw new FileNotFoundException(s_profilerModulePath.Value);
            }

            Assembly assembly = type.Assembly;
            lock (s_registeredAssembliesLocker)
            {
                if (s_registeredAssemblies.Contains(assembly))
                {
                    return;
                }

                s_registeredAssemblies.Add(assembly);
                NativeLibrary.SetDllImportResolver(assembly, ResolveProfilerDllImport);
            }
        }

        public static void InitializeResolver<T>()
        {
            InitializeResolver(typeof(T));
        }

        private static IntPtr ResolveProfilerDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // DllImport for Windows automatically loads in-memory modules (such as the profiler). This is not the case for Linux/MacOS.
            // If we fail resolving the DllImport, we have to load the profiler ourselves.
            if (s_profilerModulePath.Value == null ||
                libraryName != ProfilerIdentifiers.LibraryRootFileName)
            {
                return IntPtr.Zero;
            }

            if (NativeLibrary.TryLoad(s_profilerModulePath.Value, out IntPtr handle))
            {
                return handle;
            }

            return IntPtr.Zero;
        }
    }
}
