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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    internal sealed class MonitorRunner : BaseRunner
    {
        // Completion source containing the bound address of the default URL (e.g. provided by --urls argument)
        private readonly TaskCompletionSource<string> _defaultAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completion source containing the bound address of the metrics URL (e.g. provided by --metricUrls argument)
        private readonly TaskCompletionSource<string> _metricsAddressSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly string _runnerTmpPath =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));

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
            : base(CreateRunnerOptions(outputHelper))
        {
            Directory.CreateDirectory(SharedConfigDirectoryPath);
            Directory.CreateDirectory(UserConfigDirectoryPath);
        }

        protected override void OnCancel(CancellationToken token)
        {
            _defaultAddressSource.TrySetCanceled(token);
            _metricsAddressSource.TrySetCanceled(token);

            base.OnCancel(token);
        }

        protected override IEnumerable<string> GetProcessArguments()
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

            return argsList;
        }

        protected override IDictionary<string, string> GetProcessEnvironment()
        {
            IDictionary<string, string> environment = base.GetProcessEnvironment();

            // Console output in JSON for easy parsing
            environment.Add("Logging__Console__FormatterName", "json");
            // Enable Debug on Startup class to get lifetime and address events
            environment.Add("Logging__LogLevel__Microsoft.Diagnostics.Tools.Monitor.Startup", "Debug");

            // Override the shared config directory
            environment.Add("DotnetMonitorTestSettings__SharedConfigDirectoryOverride", SharedConfigDirectoryPath);
            // Override the user config directory
            environment.Add("DotnetMonitorTestSettings__UserConfigDirectoryOverride", UserConfigDirectoryPath);

            // Set configuration via environment variables
            var configurationViaEnvironment = ConfigurationFromEnvironment.ToEnvironmentConfiguration();
            if (configurationViaEnvironment.Count > 0)
            {
                LogLine("Environment Configuration:");
                // Set additional environment variables from configuration
                foreach (var variable in configurationViaEnvironment)
                {
                    LogLine("- {0} = {1}", variable.Key, variable.Value);
                    environment.Add(variable.Key, variable.Value);
                }
            }

            LogLine("User Settings Path: {0}", UserSettingsFilePath);

            return environment;
        }

        protected override void OnDispose()
        {
            try
            {
                Directory.Delete(_runnerTmpPath, recursive: true);
            }
            catch (Exception ex)
            {
                LogLine("Unable to delete '{0}': {1}", _runnerTmpPath, ex);
            }

            base.OnDispose();
        }

        protected override void OnStandardOutputLine(string line)
        {
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

        public Task<string> GetDefaultAddressAsync(CancellationToken token)
        {
            return GetCompletionSourceResultAsync(_defaultAddressSource, token);
        }

        public async Task<string> GetDefaultAddressAsync(TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            return await GetDefaultAddressAsync(cancellation.Token).ConfigureAwait(false);
        }

        public Task<string> GetMetricsAddressAsync(CancellationToken token)
        {
            return GetCompletionSourceResultAsync(_metricsAddressSource, token);
        }

        public async Task<string> GetMetricsAddressAsync(TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            return await GetMetricsAddressAsync(cancellation.Token).ConfigureAwait(false);
        }

        public void WriteKeyPerValueConfiguration(RootOptions options)
        {
            foreach (KeyValuePair<string, string> entry in options.ToKeyPerFileConfiguration())
            {
                File.WriteAllText(
                    Path.Combine(SharedConfigDirectoryPath, entry.Key),
                    entry.Value);

                LogLine("Wrote {0} key-per-file.", entry.Key);
            }
        }

        public async Task WriteUserSettingsAsync(RootOptions options, CancellationToken token)
        {
            using FileStream stream = new(UserSettingsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            JsonSerializerOptions serializerOptions = new()
            {
                IgnoreNullValues = true
            };

            await JsonSerializer.SerializeAsync(stream, options, serializerOptions).ConfigureAwait(false);

            LogLine("Wrote user settings.");
        }

        public async Task WriteUserSettingsAsync(RootOptions options, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await WriteUserSettingsAsync(options, cancellation.Token).ConfigureAwait(false);
        }

        private static RunnerOptions CreateRunnerOptions(ITestOutputHelper outputHelper)
        {
            if (null == outputHelper)
            {
                throw new ArgumentNullException(nameof(outputHelper));
            }

            return new RunnerOptions()
            {
                EntrypointAssemblyPath = DotNetMonitorPath,
                LogPrefix = "Monitor",
                OutputHelper = outputHelper
            };
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
                        Assert.True(TrySetStarted());
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
                        LogLine("Default Address: {0}", defaultAddress);
                        Assert.True(_defaultAddressSource.TrySetResult(defaultAddress));
                    }
                    break;
                case 17: // Bound metrics address: {address}
                    if (logEvent.State.TryGetValue("address", out string metricsAddress))
                    {
                        LogLine("Metrics Address: {0}", metricsAddress);
                        Assert.True(_metricsAddressSource.TrySetResult(metricsAddress));
                    }
                    break;
            }
        }
    }
}
