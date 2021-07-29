// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    internal sealed class MonitorRunner : IAsyncDisposable
    {
        private readonly LoggingRunnerAdapter _adapter;

        // Completion source containing the bound address of the default URL (e.g. provided by --urls argument)
        private readonly TaskCompletionSource<string> _defaultAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the bound address of the metrics URL (e.g. provided by --metricUrls argument)
        private readonly TaskCompletionSource<string> _metricsAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the string representing the base64 encoded MonitorApiKey for accessing the monitor (e.g. provided by --temp-apikey argument)
        private readonly TaskCompletionSource<string> _monitorApiKeySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly ITestOutputHelper _outputHelper;

        private readonly TaskCompletionSource<string> _readySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly DotNetRunner _runner = new();

        private readonly string _runnerTmpPath =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));

        private bool _isDisposed;

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
        /// Sets configuration values via environment variables.
        /// </summary>
        public RootOptions ConfigurationFromEnvironment { get; } = new();

        /// <summary>
        /// The mode of the diagnostic port connection. Default is <see cref="DiagnosticPortConnectionMode.Connect"/>
        /// (the tool is searching for apps that are in listen mode).
        /// </summary>
        /// <remarks>
        /// Set to <see cref="DiagnosticPortConnectionMode.Listen"/> if tool needs to establish the diagnostic port listener.
        /// </remarks>
        public DiagnosticPortConnectionMode ConnectionMode { get; set; } = DiagnosticPortConnectionMode.Connect;

        /// <summary>
        /// Path of the diagnostic port to establish when <see cref="ConnectionMode"/> is <see cref="DiagnosticPortConnectionMode.Listen"/>.
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

        private string SharedConfigDirectoryPath =>
            Path.Combine(_runnerTmpPath, "SharedConfig");

        private string UserConfigDirectoryPath =>
            Path.Combine(_runnerTmpPath, "UserConfig");

        private string UserSettingsFilePath =>
            Path.Combine(UserConfigDirectoryPath, "settings.json");

        public MonitorRunner(ITestOutputHelper outputHelper)
        {
            _outputHelper = new PrefixedOutputHelper(outputHelper, "[Monitor] ");

            // Must tell runner this is an ASP.NET Core app so that it can choose
            // the correct ASP.NET Core version (which can be different than the .NET
            // version, especially for prereleases).
            _runner.FrameworkReference = DotNetFrameworkReference.Microsoft_AspNetCore_App;

            _adapter = new LoggingRunnerAdapter(_outputHelper, _runner);
            _adapter.ReceivedStandardOutputLine += StandardOutputCallback;

            Directory.CreateDirectory(SharedConfigDirectoryPath);
            Directory.CreateDirectory(UserConfigDirectoryPath);
        }

        public async ValueTask DisposeAsync()
        {
            lock (_adapter)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }

            _adapter.ReceivedStandardOutputLine -= StandardOutputCallback;

            await _adapter.DisposeAsync().ConfigureAwait(false);

            CancelCompletionSources(CancellationToken.None);

            _runner.Dispose();

            try
            {
                Directory.Delete(_runnerTmpPath, recursive: true);
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine("Unable to delete '{0}': {1}", _runnerTmpPath, ex);
            }
        }

        public async Task StartAsync(CancellationToken token)
        {
            List<string> argsList = new();

            argsList.Add("collect");

            argsList.Add("--urls");
            argsList.Add("http://127.0.0.1:0");

            if (DisableMetricsViaCommandLine)
            {
                argsList.Add("--metrics:false");
            }
            else
            {
                argsList.Add("--metricUrls");
                argsList.Add("http://127.0.0.1:0");
            }

            if (ConnectionMode == DiagnosticPortConnectionMode.Listen)
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

            if (UseTempApiKey)
            {
                argsList.Add("--temp-apikey");
            }

            _runner.EntrypointAssemblyPath = DotNetMonitorPath;
            _runner.Arguments = string.Join(" ", argsList);

            // Disable diagnostics on tool
            _adapter.Environment.Add("COMPlus_EnableDiagnostics", "0");
            // Console output in JSON for easy parsing
            _adapter.Environment.Add("Logging__Console__FormatterName", "json");
            // Enable Information on ASP.NET Core logs for better ability to diagnose issues.
            _adapter.Environment.Add("Logging__LogLevel__Microsoft.AspNetCore", "Information");
            // Enable Debug on Startup class to get lifetime and address events
            _adapter.Environment.Add("Logging__LogLevel__Microsoft.Diagnostics.Tools.Monitor.Startup", "Debug");

            // Override the shared config directory
            _adapter.Environment.Add("DotnetMonitorTestSettings__SharedConfigDirectoryOverride", SharedConfigDirectoryPath);
            // Override the user config directory
            _adapter.Environment.Add("DotnetMonitorTestSettings__UserConfigDirectoryOverride", UserConfigDirectoryPath);

            // Set configuration via environment variables
            var configurationViaEnvironment = ConfigurationFromEnvironment.ToEnvironmentConfiguration();
            if (configurationViaEnvironment.Count > 0)
            {
                // Set additional environment variables from configuration
                foreach (var variable in configurationViaEnvironment)
                {
                    _adapter.Environment.Add(variable.Key, variable.Value);
                }
            }

            _outputHelper.WriteLine("User Settings Path: {0}", UserSettingsFilePath);

            await _adapter.StartAsync(token);

            using IDisposable _ = token.Register(() => CancelCompletionSources(token));

            // Await ready and exited tasks in case process exits before it is ready.
            if (_runner.ExitedTask == await Task.WhenAny(_readySource.Task, _runner.ExitedTask))
            {
                throw new InvalidOperationException("Process exited before it was ready.");
            }

            // Await ready task to check if it faulted or cancelled.
            await _readySource.Task;
        }

        private void CancelCompletionSources(CancellationToken token)
        {
            _defaultAddressSource.TrySetCanceled(token);
            _metricsAddressSource.TrySetCanceled(token);
            _readySource.TrySetCanceled(token);
        }

        private void StandardOutputCallback(string line)
        {
            ConsoleLogEvent logEvent = JsonSerializer.Deserialize<ConsoleLogEvent>(line);

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

        public Task<string> GetDefaultAddressAsync(CancellationToken token)
        {
            return _defaultAddressSource.GetAsync(token);
        }

        public Task<string> GetMetricsAddressAsync(CancellationToken token)
        {
            return _metricsAddressSource.GetAsync(token);
        }

        public Task<string> GetMonitorApiKey(CancellationToken token)
        {
            return _monitorApiKeySource.GetAsync(token);
        }

        public void WriteKeyPerValueConfiguration(RootOptions options)
        {
            foreach (KeyValuePair<string, string> entry in options.ToKeyPerFileConfiguration())
            {
                File.WriteAllText(
                    Path.Combine(SharedConfigDirectoryPath, entry.Key),
                    entry.Value);

                _outputHelper.WriteLine("Wrote {0} key-per-file.", entry.Key);
            }
        }

        public async Task WriteUserSettingsAsync(RootOptions options, CancellationToken token)
        {
            using FileStream stream = new(UserSettingsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            JsonSerializerOptions serializerOptions = new()
            {
#if NET6_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
#else
                IgnoreNullValues = true
#endif
            };

            await JsonSerializer.SerializeAsync(stream, options, serializerOptions).ConfigureAwait(false);

            _outputHelper.WriteLine("Wrote user settings.");
        }

        public async Task WriteUserSettingsAsync(RootOptions options, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await WriteUserSettingsAsync(options, cancellation.Token).ConfigureAwait(false);
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
            switch (logEvent.EventId)
            {
                case 16: // Bound default address: {address}
                    if (logEvent.State.TryGetValue("address", out string defaultAddress))
                    {
                        _outputHelper.WriteLine("Default Address: {0}", defaultAddress);
                        Assert.True(_defaultAddressSource.TrySetResult(defaultAddress));
                    }
                    break;
                case 17: // Bound metrics address: {address}
                    if (logEvent.State.TryGetValue("address", out string metricsAddress))
                    {
                        _outputHelper.WriteLine("Metrics Address: {0}", metricsAddress);
                        Assert.True(_metricsAddressSource.TrySetResult(metricsAddress));
                    }
                    break;
                case 23:
                    if (logEvent.State.TryGetValue("MonitorApiKey", out string monitorApiKey))
                    {
                        _outputHelper.WriteLine("MonitorApiKey: {0}", monitorApiKey);
                        Assert.True(_monitorApiKeySource.TrySetResult(monitorApiKey));
                    }
                    break;
            }
        }
    }
}
