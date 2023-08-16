// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal static class ProfilerResolver
    {
        private static readonly Lazy<string?> s_profilerModulePath = new Lazy<string?>(() => Environment.GetEnvironmentVariable(ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ModulePath));
        private static readonly Lazy<bool> s_profilerModuleExists = new Lazy<bool>(() => File.Exists(s_profilerModulePath.Value));

        private static readonly Lazy<string?> s_mutatingProfilerModulePath = new Lazy<string?>(() => Environment.GetEnvironmentVariable(ProfilerIdentifiers.MutatingProfiler.EnvironmentVariables.ModulePath));
        private static readonly Lazy<bool> s_mutatingProfilerModuleExists = new Lazy<bool>(() => File.Exists(s_mutatingProfilerModulePath.Value));

        private static readonly HashSet<Assembly> s_registeredAssemblies = new HashSet<Assembly>();
        private static readonly object s_registeredAssembliesLocker = new();

        public static void InitializeResolver(Type type)
        {
            if (s_profilerModulePath.Value != null && !s_profilerModuleExists.Value)
            {
                throw new FileNotFoundException(s_profilerModulePath.Value);
            }

            if (s_mutatingProfilerModulePath.Value != null && !s_mutatingProfilerModuleExists.Value)
            {
                throw new FileNotFoundException(s_mutatingProfilerModulePath.Value);
            }

            Assembly assembly = type.Assembly;
            lock (s_registeredAssembliesLocker)
            {
                if (!s_registeredAssemblies.Add(assembly))
                {
                    return;
                }

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
            if (s_profilerModulePath.Value != null && libraryName == ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName)
            {
                if (NativeLibrary.TryLoad(s_profilerModulePath.Value, out IntPtr handle))
                {
                    return handle;
                }
            }
            else if (s_mutatingProfilerModulePath.Value != null && libraryName == ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName)
            {
                if (NativeLibrary.TryLoad(s_mutatingProfilerModulePath.Value, out IntPtr handle))
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }
    }
}
