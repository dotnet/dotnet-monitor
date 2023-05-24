﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IDisposable
    {
        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterFunctionProbe(ulong enterProbeId);

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeUninstallation();

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeInstallation(
            [MarshalAs(UnmanagedType.LPArray)] ulong[] funcIds,
            uint count,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokens,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokenCounts);


        private readonly InstrumentedMethodCache _instrumentedMethodCache = new();
        private readonly object _requestLocker = new();
        private long _disposedState;
        private readonly bool _isAvailable;

        private readonly ILogger? _logger;
        private readonly string? _profilerModulePath;

        public ParameterCapturingService(IServiceProvider services)
        {
            _logger = services.GetService<ILogger<ParameterCapturingService>>();
            if (_logger == null)
            {
                return;
            }

            try
            {
                _profilerModulePath = Environment.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ModulePath);
                if (!File.Exists(_profilerModulePath))
                {
                    return;
                }

                NativeLibrary.SetDllImportResolver(typeof(ParameterCapturingService).Assembly, ResolveDllImport);

                RegisterFunctionProbe(FunctionProbesStub.GetProbeFunctionId());
                FunctionProbesStub.Instance = new LogEmittingProbes(_logger, _instrumentedMethodCache);

                _isAvailable = true;
            }
            catch
            {
                // TODO: Log
            }
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

        public void StopCapturing()
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException();
            }

            lock (_requestLocker)
            {
                _instrumentedMethodCache.Clear();
                RequestFunctionProbeUninstallation();
            }
        }

        public void StartCapturing(IList<MethodInfo> methods)
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException();
            }

            if (methods.Count == 0)
            {
                throw new ArgumentException(nameof(methods));
            }

            lock (_requestLocker)
            {
                _instrumentedMethodCache.Clear();
                List<ulong> functionIds = new(methods.Count);
                List<uint> argumentCounts = new(methods.Count);
                List<uint> boxingTokens = new();

                foreach (MethodInfo method in methods)
                {
                    ulong functionId = method.GetFunctionId();
                    if (functionId == 0)
                    {
                        return;
                    }

                    uint[] methodBoxingTokens = BoxingTokens.GetBoxingTokens(method);
                    if (!_instrumentedMethodCache.TryAdd(method, methodBoxingTokens))
                    {
                        return;
                    }

                    functionIds.Add(functionId);
                    argumentCounts.Add((uint)methodBoxingTokens.Length);
                    boxingTokens.AddRange(methodBoxingTokens);
                }

                RequestFunctionProbeInstallation(
                    functionIds.ToArray(),
                    (uint)functionIds.Count,
                    boxingTokens.ToArray(),
                    argumentCounts.ToArray());
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isAvailable)
            {
                return Task.CompletedTask;
            }

            return Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            if (_isAvailable)
            {
                try
                {
                    FunctionProbesStub.Instance = null;
                    StopCapturing();
                }
                catch
                {

                }
            }

            base.Dispose();
        }
    }
}
