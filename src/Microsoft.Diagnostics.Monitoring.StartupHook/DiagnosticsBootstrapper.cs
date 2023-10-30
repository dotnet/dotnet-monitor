// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using System;
using System.IO;
using MessageDispatcher = Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class DiagnosticsBootstrapper :
        IDisposable
    {
        private readonly CurrentAppDomainExceptionProcessor _exceptionProcessor = new();
        private readonly AspNetHostingStartupLoader? _hostingStartupLoader;

        private long _disposedState;

        public DiagnosticsBootstrapper()
        {
            string? hostingStartupPath = Environment.GetEnvironmentVariable(StartupHookIdentifiers.EnvironmentVariables.HostingStartupPath);
            // TODO: Log if specified hosting startup assembly doesn't exist
            if (File.Exists(hostingStartupPath))
            {
                _hostingStartupLoader = new AspNetHostingStartupLoader(hostingStartupPath);
            }

            _exceptionProcessor.Start();

            try
            {
                // Check that the profiler is loaded before establishing the dispatcher, which has a dependency on the existance of the profiler
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion)))
                {
                    SharedInternals.MessageDispatcher = new MessageDispatcher.MonitorMessageDispatcher(new MessageDispatcher.ProfilerMessageSource());
                    ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging);
                }
            }
            catch
            {
            }

            ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.StartupHook);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _exceptionProcessor.Dispose();
            _hostingStartupLoader?.Dispose();
            SharedInternals.MessageDispatcher?.Dispose();
        }
    }
}
