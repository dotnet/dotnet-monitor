// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IDisposable
    {
        private long _disposedState;
        private readonly bool _isAvailable;

        private readonly FunctionProbesManager? _probeManager;
        private readonly ILogger? _logger;

        public ParameterCapturingService(IServiceProvider services)
        {
            _logger = services.GetService<ILogger<ParameterCapturingService>>();
            if (_logger == null)
            {
                return;
            }

            try
            {
                SharedInternals.MessageLoop.RegisterCallback<ParameterCapturingPayload>(ProfilerCommand.CaptureParameter, OnCommand);
                _probeManager = new FunctionProbesManager(new LogEmittingProbes(_logger, FunctionProbesStub.InstrumentedMethodCache));
                _isAvailable = true;
            }
            catch
            {
                // TODO: Log
            }
        }

        private void OnCommand(ParameterCapturingPayload request)
        {
            if (request.FqMethodNames.Length == 0)
            {
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<MethodInfo> methods = new(request.FqMethodNames.Length);
            foreach (string methodName in request.FqMethodNames)
            {
                MethodInfo? methodInfo = ResolveMethod(assemblies, methodName);
                if (methodInfo == null)
                {
                    return;
                }

                methods.Add(methodInfo);
            }

            StartCapturing(methods, request.Duration);
        }

        private static MethodInfo? ResolveMethod(Assembly[] assemblies, string fqMethodName)
        {
            int dllSplitIndex = fqMethodName.IndexOf('!');
            string dll = fqMethodName[..dllSplitIndex];
            string classAndMethod = fqMethodName[(dllSplitIndex + 1)..];
            int lastIndex = classAndMethod.LastIndexOf('.');

            string className = classAndMethod[..lastIndex];
            string methodName = classAndMethod[(lastIndex + 1)..];


            Module? userMod = null;
            Assembly? userAssembly = null;
            foreach (var assembly in assemblies)
            {
                foreach (var mod in assembly.Modules)
                {
                    if (mod.Name == dll)
                    {
                        userAssembly = assembly;
                        userMod = mod;
                        break;
                    }
                }
            }

            if (userMod == null || userAssembly == null)
            {
                return null;
            }

            Type? remoteClass = userAssembly.GetType(className);
            if (remoteClass == null)
            {
                return null;
            }

            MethodInfo? methodInfo = remoteClass.GetMethod(methodName);
            if (methodInfo == null)
            {
                return null;
            }

            return methodInfo;
        }

        public void StopCapturing()
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException();
            }

            _probeManager?.StopCapturing();
        }

        public void StartCapturing(IList<MethodInfo> methods, TimeSpan duratation)
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException();
            }

            _probeManager?.StartCapturing(methods);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isAvailable)
            {
                return;
            }

            TimeSpan delay = Timeout.InfiniteTimeSpan;
            while(!stoppingToken.IsCancellationRequested)
            {
                // Wait for a new request
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                StopCapturing();
            }

            return;
        }

        public override void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            try
            {
                SharedInternals.MessageLoop.UnregisterCallback(ProfilerCommand.CaptureParameter);
                _probeManager?.Dispose();
            }
            catch
            {

            }

            base.Dispose();
        }
    }
}
