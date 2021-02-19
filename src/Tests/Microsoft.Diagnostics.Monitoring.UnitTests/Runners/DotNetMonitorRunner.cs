// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    /// <summary>
    /// Runner for running dotnet-monitor tool.
    /// </summary>
    internal sealed class DotNetMonitorRunner : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation =
            new CancellationTokenSource();
        
        // Cancellation registration used to unregister that cancellation callback
        // that cancels the TaskCompletionSource<T> fields.
        private readonly IDisposable _cancellationRegistration;

        // Completion source containing the bound address of the default URL (e.g. provided by --urls argument)
        private readonly TaskCompletionSource<string> _defaultAddressSource =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the bound address of the metrics URL (e.g. provided by --metricUrls argument)
        private readonly TaskCompletionSource<string> _metricsAddressSource =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Completion source signaled when dotnet-monitor is running
        private readonly TaskCompletionSource<string> _startedSource =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly ITestOutputHelper _outputHelper;

        // DotNetRunner will run dotnet-monitor with an explicit framework version
        // that corresponds to the target framework of the assembly in which
        // DorNetRunner exists. This allows for testing dotnet-monitor on all framework
        // versions of .NET Core 3.1 and higher, thus explicitly testing roll-forward.
        private readonly DotNetRunner _runner = new DotNetRunner();

        private bool _isDisposed;

        // Task that processes the stdout of dotnet-monitor for significant events.
        private Task<Task> _processStandardOutputTask;

        /// <summary>
        /// The path of the currently executing assembly.
        /// </summary>
        private static string CurrentExecutingAssemblyPath =>
            Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// The target framework name of the currently executing assembly.
        /// </summary>
        private static string CurrentTargetFrameworkFolderName =>
            new FileInfo(CurrentExecutingAssemblyPath).Directory.Name;

        /// <summary>
        /// The path to dotnet-monitor. It is currently only build for the
        /// netcoreapp3.1 target framework.
        /// </summary>
        private static string DotNetMonitorPath =>
            CurrentExecutingAssemblyPath
                .Replace(Assembly.GetExecutingAssembly().GetName().Name, "dotnet-monitor")
                .Replace(CurrentTargetFrameworkFolderName, "netcoreapp3.1");

        /// <summary>
        /// Task that completes with the bound address of the default URL.
        /// </summary>
        public Task<string> DefaultAddressTask => _defaultAddressSource.Task;

        /// <summary>
        /// Task that completes with the bound address of the metrics URL.
        /// </summary>
        public Task<string> MetricsAddressTask => _metricsAddressSource.Task;

        /// <summary>
        /// Determines whether authentication is disabled when starting dotnet-monitor.
        /// </summary>
        public bool NoAuthentication { get; set; }

        public DotNetMonitorRunner(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));

            CancellationToken token = _cancellation.Token;
            _cancellationRegistration = token.Register(() => Cancel(token));

            _processStandardOutputTask = new Task<Task>(
                () => ProcessStandardOutputAsync(_cancellation.Token),
                _cancellation.Token,
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Starts dotnet-monitor process and waits for application startup.
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            IList<string> argsList = new List<string>();
            argsList.Add("collect");
            argsList.Add("--urls");
            argsList.Add("http://127.0.0.1:0");
            argsList.Add("--metricUrls");
            argsList.Add("http://127.0.0.1:0");
            if (NoAuthentication)
            {
                argsList.Add("--no-auth");
            }
            string args = string.Join(" ", argsList);

            _outputHelper.WriteLine("Monitor Path: {0}", DotNetMonitorPath);
            _outputHelper.WriteLine("Monitor Args: {0}", args);

            _runner.EntryAssemblyPath = DotNetMonitorPath;
            _runner.Arguments = args;

            // Do not want to diagnose self
            _runner.SetEnvironmentVariable("COMPlus_EnableDiagnostics", "0");
            // Console output in JSON for easy parsing
            _runner.SetEnvironmentVariable("Logging__Console__FormatterName", "json");
            // Enable Debug on Startup class to get lifetime and address events
            _runner.SetEnvironmentVariable("Logging__LogLevel__Microsoft.Diagnostics.Tools.Monitor.Startup", "Debug");

            // Start running dotnet-monitor
            await _runner.StartAsync(token);

            // Cancel the completion sources if cancellation is requested
            using var _ = token.Register(() => Cancel(token));

            // Start processing stdout
            _processStandardOutputTask.Start();

            // Write some diagnostic information provided by stdout
            _outputHelper.WriteLine("Default Address: {0}", await DefaultAddressTask);
            _outputHelper.WriteLine("Metrics Address: {0}", await MetricsAddressTask);

            await _startedSource.Task;
        }

        public async Task StartAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
            await StartAsync(timeoutSource.Token);
        }

        /// <summary>
        /// Shuts down dotnet-monitor process.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            lock (_cancellation)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }

            // Shutdown the tool
            _runner.ForceClose();

            // Wait for it to exit
            int exitCode = await _runner.WaitForExitAsync(CancellationToken.None).SafeAwait(-1);
            _outputHelper.WriteLine("Monitor Exit Code: {0}", exitCode);

            // Cancel any remaining tasks
            _cancellation.Cancel();

            // Wait for stdout processing to finish
            await _processStandardOutputTask.Unwrap().SafeAwait();

            // Dispose cancellation registrations
            _cancellationRegistration.Dispose();

            _cancellation.Dispose();
        }

        private async Task ProcessStandardOutputAsync(CancellationToken token)
        {
            try
            {
                TaskCompletionSource<string> cancellationTaskSource = new TaskCompletionSource<string>();
                using var _ = token.Register(() => cancellationTaskSource.TrySetCanceled(token));

                while (true)
                {
                    // ReadLineAsync does not have cancellation
                    string line = await Task.WhenAny(
                        _runner.StandardOutput.ReadLineAsync(),
                        cancellationTaskSource.Task)
                        .Unwrap();

                    if (null == line)
                        break;

                    LogEvent logEvent = JsonSerializer.Deserialize<LogEvent>(line);

                    switch (logEvent.Category)
                    {
                        case "Microsoft.Hosting.Lifetime":
                            HandleLifetimeEvent(logEvent);
                            break;
                        case "Microsoft.Diagnostics.Tools.Monitor.Startup":
                            HandleStartupEvent(logEvent);
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HandleLifetimeEvent(LogEvent logEvent)
        {
            // Lifetime events do not have unique EventIds, thus use the format
            // string to differentiate the individual events.
            if (logEvent.State.TryGetValue("{OriginalFormat}", out string format))
            {
                switch (format)
                {
                    case "Application started. Press Ctrl+C to shut down.":
                        _startedSource.SetResult(null);
                        break;
                }
            }
        }

        private void HandleStartupEvent(LogEvent logEvent)
        {
            switch (logEvent.EventId)
            {
                case 16: // Bound default address: {address}
                    if (logEvent.State.TryGetValue("address", out string defaultAddress))
                    {
                        _defaultAddressSource.SetResult(defaultAddress);
                    }
                    break;
                case 17: // Bound metrics address: {address}
                    if (logEvent.State.TryGetValue("address", out string metricsAddress))
                    {
                        _metricsAddressSource.SetResult(metricsAddress);
                    }
                    break;
            }
        }

        private void Cancel(CancellationToken token)
        {
            _defaultAddressSource.TrySetCanceled(token);
            _metricsAddressSource.TrySetCanceled(token);
            _startedSource.TrySetCanceled(token);
        }

        // All log events have this structure (plus additional fields
        // not needed by the test runner for identifying events).
        private class LogEvent
        {
            public string Category { get; set; }

            public int EventId { get; set; }

            public Dictionary<string, string> State { get; set; }
        }
    }
}
