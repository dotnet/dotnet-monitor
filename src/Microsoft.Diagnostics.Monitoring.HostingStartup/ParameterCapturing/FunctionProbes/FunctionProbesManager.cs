// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class FunctionProbesManager : IDisposable
    {
        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeRegistration(ulong enterProbeId);

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeUninstallation();

        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeInstallation(
            [MarshalAs(UnmanagedType.LPArray)] ulong[] funcIds,
            uint count,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokens,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokenCounts);
       
        private readonly object _requestLocker = new();
        private long _disposedState;

        public FunctionProbesManager(IFunctionProbes probes)
        {
            ProfilerResolver.InitializeResolver<FunctionProbesManager>();

            RequestFunctionProbeRegistration(FunctionProbesStub.GetProbeFunctionId());
            FunctionProbesStub.Instance = probes;
        }

        public void StopCapturing()
        {
            lock (_requestLocker)
            {
                FunctionProbesStub.InstrumentedMethodCache.Clear();
                RequestFunctionProbeUninstallation();
            }
        }

        public void StartCapturing(IList<MethodInfo> methods)
        {
            if (methods.Count == 0)
            {
                throw new ArgumentException(nameof(methods));
            }

            lock (_requestLocker)
            {
                FunctionProbesStub.InstrumentedMethodCache.Clear();
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
                    if (!FunctionProbesStub.InstrumentedMethodCache.TryAdd(method, methodBoxingTokens))
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

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            FunctionProbesStub.Instance = null;
            StopCapturing();
        }
    }
}
