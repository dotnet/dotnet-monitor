// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    internal sealed partial class MonitorCollectRunner : MonitorRunner
    {
        // Completion source containing the bound address of the default URL (e.g. provided by --urls argument)
        private readonly TaskCompletionSource<string> _defaultAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the bound address of the metrics URL (e.g. provided by --metricUrls argument)
        private readonly TaskCompletionSource<string> _metricsAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the string representing the base64 encoded MonitorApiKey for accessing the monitor (e.g. provided by --temp-apikey argument)
        private readonly TaskCompletionSource<string> _monitorApiKeySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing a string which is fired when the monitor enters a ready idle state
        private readonly TaskCompletionSource<string> _readySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private long _disposedState;

        /// <summary>
        /// Event callback for when a Private Key warning message is seen.
        /// </summary>
        public event Action<string> WarnPrivateKey;

        /// <summary>
        /// The mode of the diagnostic port connection.
        /// </summary>
        /// <remarks>
        /// Set to <see cref="DiagnosticPortConnectionMode.Connect"/> if tool needs to discover the diagnostic port for each target process.
        /// Set to <see cref="DiagnosticPortConnectionMode.Listen"/> if tool needs to establish the diagnostic port listener.
        /// </remarks>
        public DiagnosticPortConnectionMode? ConnectionModeViaCommandLine { get; set; }

        /// <summary>
        /// Path of the diagnostic port to establish when <see cref="ConnectionModeViaCommandLine"/> is <see cref="DiagnosticPortConnectionMode.Listen"/>.
        /// </summary>
        public string DiagnosticPortPath { get; set; }

        /// <summary>
        /// Determines whether authentication is disabled when starting dotnet-monitor.
        /// </summary>
        public bool DisableAuthentication { get; set; }

        /// <summary>
        /// Determines whether HTTP egress is disabled when starting dotnet-monitor.
        /// </summary>
        public bool DisableHttpEgress { get; set; }

        /// <summary>
        /// Determines whether a temporary api key should be generated while starting dotnet-monitor.
        /// </summary>
        public bool UseTempApiKey { get; set; }

        /// <summary>
        /// Determines whether metrics are disabled via the command line when starting dotnet-monitor.
        /// </summary>
        public bool DisableMetricsViaCommandLine { get; set; }

        /// <summary>
        /// Reports whether the server URLs were overridden by UseKestrel/ConfigureKestrel code.
        /// </summary>
        public bool OverrodeServerUrls { get; private set; }

        /// <summary>
        /// Urls used for the DOTNET_Urls environment variable.
        /// </summary>
        public string DotNetUrls { get; set; }

        /// <summary>
        /// Urls used for the ASPNETCORE_Urls environment variable.
        /// </summary>
        public string AspNetCoreUrls { get; set; }

        /// <summary>
        /// Urls used for the DOTNETMONITOR_Urls environment variable.
        /// </summary>
        public string DotNetMonitorUrls { get; set; }

        /// <summary>
        /// Determines whether the dotnet-monitor process should exit when stdin is disconnected.
        /// </summary>
        public bool ExitOnStdinDisconnect { get; set; }

        public MonitorCollectRunner(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        public override async ValueTask DisposeAsync()
        {
            if (!TestCommon.DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            CancelCompletionSources(CancellationToken.None);

            await base.DisposeAsync();
        }

        public async Task StartAsync(CancellationToken token)
        {
            List<string> argsList = new();

            const string command = "collect";

            argsList.Add("--urls");
            argsList.Add("http://127.0.0.1:0");

            if (!string.IsNullOrEmpty(DotNetUrls))
            {
                SetEnvironmentVariable("DOTNET_Urls", DotNetUrls);
            }
            if (!string.IsNullOrEmpty(AspNetCoreUrls))
            {
                SetEnvironmentVariable("ASPNETCORE_Urls", AspNetCoreUrls);
            }
            if (!string.IsNullOrEmpty(DotNetMonitorUrls))
            {
                SetEnvironmentVariable("DOTNETMONITOR_Urls", DotNetMonitorUrls);
            }

            if (DisableMetricsViaCommandLine)
            {
                argsList.Add("--metrics:false");
            }
            else
            {
                argsList.Add("--metricUrls");
                argsList.Add("http://127.0.0.1:0");
            }

            if (ConnectionModeViaCommandLine == DiagnosticPortConnectionMode.Listen)
            {
                argsList.Add("--diagnostic-port");
                if (string.IsNullOrEmpty(DiagnosticPortPath))
                {
                    throw new ArgumentNullException(nameof(DiagnosticPortPath));
                }
                argsList.Add(DiagnosticPortPath);
            }

            if (DisableAuthentication)
            {
                argsList.Add("--no-auth");
            }

            if (DisableHttpEgress)
            {
                argsList.Add("--no-http-egress");
            }

            if (UseTempApiKey)
            {
                argsList.Add("--temp-apikey");
            }

            if (ExitOnStdinDisconnect)
            {
                argsList.Add("--exit-on-stdin-disconnect");
            }

            using IDisposable _ = token.Register(() => CancelCompletionSources(token));

            await base.StartAsync(command, argsList.ToArray(), token);

            Task<int> runnerExitTask = RunnerExitedTask;
            Task endingTask = await Task.WhenAny(_readySource.Task, runnerExitTask);
            // Await ready and exited tasks in case process exits before it is ready.
            if (runnerExitTask == endingTask)
            {
                throw new InvalidOperationException("Process exited before it was ready.");
            }

            await _readySource.Task;
        }

        protected override void StandardOutputCallback(string line)
        {
            try
            {
                ConsoleLogEvent logEvent = JsonSerializer.Deserialize<ConsoleLogEvent>(line);

                switch (logEvent.Category)
                {
                    case "Microsoft.AspNetCore.Server.Kestrel":
                        HandleKestrelEvent(logEvent);
                        break;
                    case "Microsoft.Hosting.Lifetime":
                        HandleLifetimeEvent(logEvent);
                        break;
                    case "Microsoft.Diagnostics.Tools.Monitor.Startup":
                        HandleStartupEvent(logEvent);
                        break;
                    case "Microsoft.Diagnostics.Tools.Monitor.CollectionRules.CollectionRuleService":
                        HandleCollectionRuleEvent(logEvent);
                        break;
                    case "Microsoft.Diagnostics.Tools.Monitor.LogsOperation":
                        HandleArtifactEvent(logEvent);
                        break;
                    default:
                        HandleGenericLogEvent(logEvent);
                        break;
                }
            }
            catch (JsonException)
            {
                // Unable to parse the output. These could be lines written to stdout that are not JSON formatted.
                _outputHelper.WriteLine("Unable to JSON parse stdout line: {0}", line);
            }
        }

        private void CancelCompletionSources(CancellationToken token)
        {
            _defaultAddressSource.TrySetCanceled(token);
            _metricsAddressSource.TrySetCanceled(token);
            _readySource.TrySetCanceled(token);
            _monitorApiKeySource.TrySetCanceled(token);
        }

        public Task<string> GetDefaultAddressAsync(CancellationToken token)
        {
            return _defaultAddressSource.Task.WithCancellation(token);
        }

        public Task<string> GetMetricsAddressAsync(CancellationToken token)
        {
            return _metricsAddressSource.Task.WithCancellation(token);
        }

        public Task<string> GetMonitorApiKey(CancellationToken token)
        {
            return _monitorApiKeySource.Task.WithCancellation(token);
        }

        private void HandleLifetimeEvent(ConsoleLogEvent logEvent)
        {
            // Lifetime events do not have unique EventIds, thus use the format
            // string to differentiate the individual events.
            if (logEvent.State.TryGetValue("{OriginalFormat}", out string format))
            {
                switch (format)
                {
                    case "Application started. Press Ctrl+C to shut down.":
                        Assert.True(_readySource.TrySetResult(null));
                        break;
                }
            }
        }

        private void HandleStartupEvent(ConsoleLogEvent logEvent)
        {
            switch ((LoggingEventIds)logEvent.EventId)
            {
                case LoggingEventIds.BoundDefaultAddress:
                    if (logEvent.State.TryGetValue("address", out string defaultAddress))
                    {
                        _outputHelper.WriteLine("Default Address: {0}", defaultAddress);
                        Assert.True(_defaultAddressSource.TrySetResult(defaultAddress));
                    }
                    break;
                case LoggingEventIds.BoundMetricsAddress:
                    if (logEvent.State.TryGetValue("address", out string metricsAddress))
                    {
                        _outputHelper.WriteLine("Metrics Address: {0}", metricsAddress);
                        Assert.True(_metricsAddressSource.TrySetResult(metricsAddress));
                    }
                    break;
                case LoggingEventIds.LogTempApiKey:
                    if (logEvent.State.TryGetValue("MonitorApiKey", out string monitorApiKey))
                    {
                        _outputHelper.WriteLine("MonitorApiKey: {0}", monitorApiKey);
                        Assert.True(_monitorApiKeySource.TrySetResult(monitorApiKey));
                    }
                    break;
            }
        }

        private void HandleKestrelEvent(ConsoleLogEvent logEvent)
        {
            if (logEvent.Message.StartsWith("Overriding address(es)"))
            {
                OverrodeServerUrls = true;
            }
        }

        private void HandleGenericLogEvent(ConsoleLogEvent logEvent)
        {
            switch ((LoggingEventIds)logEvent.EventId)
            {
                case LoggingEventIds.NotifyPrivateKey:
                    if (logEvent.State.TryGetValue("fieldName", out string fieldName))
                    {
                        _outputHelper.WriteLine("Private Key data detected in field: {0}", fieldName);
                        WarnPrivateKey?.Invoke(fieldName);
                    }
                    break;
            }
        }
    }
}
