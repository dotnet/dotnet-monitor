// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class FunctionProbesManager : IFunctionProbesManager
    {
        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeRegistration(ulong enterProbeId);

        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeUninstallation();

        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RequestFunctionProbeInstallation(
            [MarshalAs(UnmanagedType.LPArray)] ulong[] funcIds,
            uint count,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokens,
            [MarshalAs(UnmanagedType.LPArray)] uint[] boxingTokenCounts);

        private delegate void FunctionProbeRegistrationCallback(int hresult);
        private delegate void FunctionProbeInstallationCallback(int hresult);
        private delegate void FunctionProbeUninstallationCallback(int hresult);
        private delegate void FunctionProbeFaultCallback(ulong uniquifier);

        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void RegisterFunctionProbeCallbacks(
            FunctionProbeRegistrationCallback onRegistration,
            FunctionProbeInstallationCallback onInstallation,
            FunctionProbeUninstallationCallback onUninstallation,
            FunctionProbeFaultCallback onFault);

        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void UnregisterFunctionProbeCallbacks();

        private long _probeState;
        private const long ProbeStateUninitialized = default(long);
        private const long ProbeStateUninstalled = 1;
        private const long ProbeStateUninstalling = 2;
        private const long ProbeStateInstalling = 3;
        private const long ProbeStateInstalled = 4;
        private const long ProbeStateUnrecoverable = 5;

        private readonly TaskCompletionSource _probeRegistrationTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource? _installationTaskSource;
        private TaskCompletionSource? _uninstallationTaskSource;

        private long _disposedState;

        public event EventHandler<ulong>? OnProbeFault;

        public FunctionProbesManager(IFunctionProbes probes)
        {
            ProfilerResolver.InitializeResolver<FunctionProbesManager>();

            RegisterFunctionProbeCallbacks(OnRegistration, OnInstallation, OnUninstallation, OnFault);
            RequestFunctionProbeRegistration(FunctionProbesStub.GetProbeFunctionId());

            FunctionProbesStub.Instance = probes;
        }

        private void OnRegistration(int hresult)
        {
            TransitionStateFromHr(_probeRegistrationTaskSource, hresult,
                expectedState: ProbeStateUninitialized,
                succeededState: ProbeStateUninstalled,
                failedState: ProbeStateUnrecoverable);
        }

        private void OnInstallation(int hresult)
        {
            TransitionStateFromHr(_installationTaskSource, hresult,
                expectedState: ProbeStateInstalling,
                succeededState: ProbeStateInstalled,
                failedState: ProbeStateUninstalled);
        }

        private void OnUninstallation(int hresult)
        {
            TransitionStateFromHr(_uninstallationTaskSource, hresult,
                expectedState: ProbeStateUninstalling,
                succeededState: ProbeStateUninstalled,
                failedState: ProbeStateInstalled);
        }

        private void OnFault(ulong uniquifier)
        {
            OnProbeFault?.Invoke(this, uniquifier);
        }
        
        private void TransitionStateFromHr(TaskCompletionSource? taskCompletionSource, int hresult, long expectedState, long succeededState, long failedState)
        {
            Exception? ex = Marshal.GetExceptionForHR(hresult);
            long newState = (ex == null) ? succeededState : failedState;

            if (expectedState != Interlocked.CompareExchange(ref _probeState, newState, expectedState))
            {
                // Unexpected, the profiler is in a different state than us.
                StateMismatch(expectedState);
                return;
            }

            if (ex == null)
            {
                _ = taskCompletionSource?.TrySetResult();
            }
            else
            {
                _ = taskCompletionSource?.TrySetException(ex);
            }
        }

        private void StateMismatch(long expected)
        {
            InvalidOperationException ex = new(string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.ErrorMessage_ProbeStateMismatchFormatString, expected, _probeState));

            _probeState = ProbeStateUnrecoverable;
            _ = _installationTaskSource?.TrySetException(ex);
            _ = _uninstallationTaskSource?.TrySetException(ex);
            _ = _probeRegistrationTaskSource?.TrySetException(ex);
        }

        public async Task StopCapturingAsync(CancellationToken token)
        {
            if (ProbeStateInstalled != Interlocked.CompareExchange(ref _probeState, ProbeStateUninstalling, ProbeStateInstalled))
            {
                throw new InvalidOperationException();
            }

            _uninstallationTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                StopCapturingCore();
            }
            catch
            {
                _uninstallationTaskSource = null;
                _probeState = ProbeStateInstalled;
                throw;
            }

            await _uninstallationTaskSource.Task.WaitAsync(token).ConfigureAwait(false);
        }

        private void StopCapturingCore()
        {
            if (_probeState == ProbeStateUninstalled)
            {
                return;
            }

            FunctionProbesStub.InstrumentedMethodCache = null;
            RequestFunctionProbeUninstallation();
        }


        public async Task StartCapturingAsync(IList<MethodInfo> methods, CancellationToken token)
        {
            if (methods.Count == 0)
            {
                throw new ArgumentException(nameof(methods));
            }

            await _probeRegistrationTaskSource.Task.WaitAsync(token).ConfigureAwait(false);

            if (ProbeStateUninstalled != Interlocked.CompareExchange(ref _probeState, ProbeStateInstalling, ProbeStateUninstalled))
            {
                throw new InvalidOperationException();
            }

            try
            {
                Dictionary<ulong, InstrumentedMethod> newMethodCache = new(methods.Count);
                List<ulong> functionIds = new(methods.Count);
                List<uint> argumentCounts = new(methods.Count);
                List<uint> boxingTokens = new();

                foreach (MethodInfo method in methods)
                {
                    ulong functionId = method.GetFunctionId();
                    if (functionId == 0)
                    {
                        throw new NotSupportedException(method.Name);
                    }

                    uint[] methodBoxingTokens = BoxingTokens.GetBoxingTokens(method);
                    if (!newMethodCache.TryAdd(functionId, new InstrumentedMethod(method, methodBoxingTokens)))
                    {
                        // Duplicate, ignore
                        continue;
                    }

                    functionIds.Add(functionId);
                    argumentCounts.Add((uint)methodBoxingTokens.Length);
                    boxingTokens.AddRange(methodBoxingTokens);
                }

                FunctionProbesStub.InstrumentedMethodCache = new ReadOnlyDictionary<ulong, InstrumentedMethod>(newMethodCache);

                _installationTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                RequestFunctionProbeInstallation(
                    functionIds.ToArray(),
                    (uint)functionIds.Count,
                    boxingTokens.ToArray(),
                    argumentCounts.ToArray());
            }
            catch
            {
                FunctionProbesStub.InstrumentedMethodCache = null;
                _probeState = ProbeStateUninstalled;
                _installationTaskSource = null;
                throw;
            }

            await _installationTaskSource.Task.WaitAsync(token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            FunctionProbesStub.Instance = null;

            _ = _installationTaskSource?.TrySetCanceled();
            _ = _uninstallationTaskSource?.TrySetCanceled();

            try
            {
                UnregisterFunctionProbeCallbacks();
                StopCapturingCore();
            }
            catch
            {
            }
        }
    }
}
