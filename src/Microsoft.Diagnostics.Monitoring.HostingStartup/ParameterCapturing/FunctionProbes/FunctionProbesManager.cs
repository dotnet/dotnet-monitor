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
            IntPtr onRegistration,
            IntPtr onInstallation,
            IntPtr onUninstallation,
            IntPtr onFault);

        [DllImport(ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
        private static extern void UnregisterFunctionProbeCallbacks();

        private readonly FunctionProbeRegistrationCallback _onRegistrationDelegate;
        private readonly FunctionProbeInstallationCallback _onInstallationDelegate;
        private readonly FunctionProbeUninstallationCallback _onUninstallationDelegate;
        private readonly FunctionProbeFaultCallback _onFaultDelegate;

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

        private readonly CancellationTokenSource _disposalTokenSource = new();
        private long _disposedState;

        public event EventHandler<InstrumentedMethod>? OnProbeFault;

        public FunctionProbesManager(IFunctionProbes probes)
        {
            ProfilerResolver.InitializeResolver<FunctionProbesManager>();

            _onRegistrationDelegate = OnRegistration;
            _onInstallationDelegate = OnInstallation;
            _onUninstallationDelegate = OnUninstallation;
            _onFaultDelegate = OnFault;

            RegisterFunctionProbeCallbacks(
                Marshal.GetFunctionPointerForDelegate(_onRegistrationDelegate),
                Marshal.GetFunctionPointerForDelegate(_onInstallationDelegate),
                Marshal.GetFunctionPointerForDelegate(_onUninstallationDelegate),
                Marshal.GetFunctionPointerForDelegate(_onFaultDelegate));

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
            var methodCache = FunctionProbesStub.InstrumentedMethodCache;
            if (methodCache == null ||
                !methodCache.TryGetValue(uniquifier, out InstrumentedMethod? instrumentedMethod))
            {
                //
                // The probe fault occurred in a method that is no longer actively instrumented, ignore.
                // This can happen when we request uninstallation of function probes and there's still a thread
                // actively in one of the instrumented methods and it happens to fault.
                //
                return;
            }

            OnProbeFault?.Invoke(this, instrumentedMethod);
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
            _ = _probeRegistrationTaskSource.TrySetException(ex);
            _ = _installationTaskSource?.TrySetException(ex);
            _ = _uninstallationTaskSource?.TrySetException(ex);
        }

        public async Task StopCapturingAsync(CancellationToken token)
        {
            DisposableHelper.ThrowIfDisposed<FunctionProbesManager>(ref _disposedState);

            if (ProbeStateInstalled != Interlocked.CompareExchange(ref _probeState, ProbeStateUninstalling, ProbeStateInstalled))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.ErrorMessage_ProbeStateMismatchFormatString, ProbeStateInstalled, _probeState));
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

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposalTokenSource.Token, token);
            using IDisposable _ = cts.Token.Register(() =>
            {
                _uninstallationTaskSource.TrySetCanceled(cts.Token);
            });
            await _uninstallationTaskSource.Task.ConfigureAwait(false);
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
            DisposableHelper.ThrowIfDisposed<FunctionProbesManager>(ref _disposedState);

            if (methods.Count == 0)
            {
                throw new ArgumentException(nameof(methods));
            }

            // _probeRegistrationTaskSource will be cancelled (if needed) on dispose
            await _probeRegistrationTaskSource.Task.WaitAsync(token).ConfigureAwait(false);

            if (ProbeStateUninstalled != Interlocked.CompareExchange(ref _probeState, ProbeStateInstalling, ProbeStateUninstalled))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.ErrorMessage_ProbeStateMismatchFormatString, ProbeStateUninstalled, _probeState));
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
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.ErrorMessage_FunctionDoesNotHaveIdFormatString, method.Name));
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

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposalTokenSource.Token, token);
            using IDisposable _ = cts.Token.Register(() =>
            {
                _installationTaskSource.TrySetCanceled(cts.Token);
            });

            await _installationTaskSource.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            try
            {
                _disposalTokenSource.Cancel();
            }
            catch
            {
            }

            _ = _probeRegistrationTaskSource.TrySetCanceled(_disposalTokenSource.Token);
            _disposalTokenSource.Dispose();

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
