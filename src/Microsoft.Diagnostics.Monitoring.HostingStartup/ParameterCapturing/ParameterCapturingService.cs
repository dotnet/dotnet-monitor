// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
                SharedInternals.MonitorMessageDispatcher.RegisterCallback<ParameterCapturingPayload>(ProfilerCommand.CaptureParameters, OnCommand);
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

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.ReflectionOnly && !assembly.IsDynamic).ToArray();

            List<MethodInfo> methods = new(request.FqMethodNames.Length);
            foreach (string methodName in request.FqMethodNames)
            {
                List<MethodInfo> resolvedMethods = ResolveMethod(assemblies, methodName);
                methods.AddRange(resolvedMethods);
            }

            StartCapturing(methods, request.Duration);
        }

        private List<MethodInfo> ResolveMethod(Assembly[] assemblies, string fqMethodName)
        {
            // JSFIX: proof-of-concept code
            int dllSplitIndex = fqMethodName.IndexOf('!');
            string dll = fqMethodName[..dllSplitIndex];
            string classAndMethod = fqMethodName[(dllSplitIndex + 1)..];
            int lastIndex = classAndMethod.LastIndexOf('.');

            string className = classAndMethod[..lastIndex];
            string methodName = classAndMethod[(lastIndex + 1)..];

            List<MethodInfo> methods = new();

            // JSFIX: Consider lookup table
            foreach (Assembly assembly in assemblies)
            {
                foreach (Module module in assembly.Modules)
                {
                    if (string.Equals(module.Name, dll, StringComparison.OrdinalIgnoreCase))
                    {
                        // JSFIX: What if there are multiple matches ... select all or none?
                        // Pick all for now.
                        try
                        {
                            MethodInfo? method = module.GetType(className)?.GetMethod(methodName);
                            if (method != null)
                            {
                                methods.Add(method);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log warning to user, they requested it.... Can we signal back to the profiler that this request failed somehow?
                            // Either a callback into the profiler, or have the profiler reverse pinvoke to dispatch the message so it can observe
                            // the result.
                            // if so, do we register a callback?
                            // JSFIX: Resx and better format string
                            _logger?.LogWarning($"Unable resolve method {fqMethodName}, exception: {ex}");
                        }

                    }
                }
            }

            return methods;
        }

        public void StopCapturing()
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException();
            }

            _probeManager?.StopCapturing();
        }

        public void StartCapturing(IList<MethodInfo> methods, TimeSpan duration)
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
                SharedInternals.MonitorMessageDispatcher.UnregisterCallback(ProfilerCommand.CaptureParameters);
                _probeManager?.Dispose();
            }
            catch
            {

            }

            base.Dispose();
        }
    }
}
