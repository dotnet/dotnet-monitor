// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    internal class MonitorRunner : IAsyncDisposable
    {
        protected readonly object _lock = new();

        protected readonly ITestOutputHelper _outputHelper;

        private readonly DotNetRunner _runner = new();

        private readonly LoggingRunnerAdapter _adapter;

        private bool _isDisposed;

        /// <summary>
        /// Sets configuration values via environment variables.
        /// </summary>
        public RootOptions ConfigurationFromEnvironment { get; } = new();

        /// <summary>
        /// Gets the task for the underlying <see cref="DotNetRunner"/>'s
        /// <see cref="DotNetRunner.ExitedTask"/> which is used to wait for process exit.
        /// </summary>
        protected Task<int> RunnerExitedTask => _runner.ExitedTask;

        /// <summary>
        /// The path to dotnet-monitor. The tool is currently built for netcoreapp3.1 and net6.0.
        /// For netcoreapp3.1 and net5.0 testing, use the netcoreapp3.1 version. For net6.0+,
        /// use the net6.0 version.
        /// </summary>
        private static string DotNetMonitorPath =>
            AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                "dotnet-monitor",
#if NET6_0_OR_GREATER
                TargetFrameworkMoniker.Net60);
#else
                TargetFrameworkMoniker.NetCoreApp31);
#endif

        private string SharedConfigDirectoryPath =>
            Path.Combine(TempPath, "SharedConfig");

        private string UserConfigDirectoryPath =>
            Path.Combine(TempPath, "UserConfig");

        private string UserSettingsFilePath =>
            Path.Combine(UserConfigDirectoryPath, "settings.json");

        public string TempPath { get; } =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));

        public MonitorRunner(ITestOutputHelper outputHelper)
        {
            _outputHelper = new PrefixedOutputHelper(outputHelper, "[Monitor] ");

            _adapter = new LoggingRunnerAdapter(_outputHelper, _runner);
            _adapter.ReceivedStandardOutputLine += StandardOutputCallback;

            Directory.CreateDirectory(SharedConfigDirectoryPath);
            Directory.CreateDirectory(UserConfigDirectoryPath);
        }

        public virtual async ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }

            _adapter.ReceivedStandardOutputLine -= StandardOutputCallback;
            await _adapter.DisposeAsync().ConfigureAwait(false);

            _runner.Dispose();

            try
            {
                Directory.Delete(TempPath, recursive: true);
            }
            catch (Exception ex)
            {
                _outputHelper.WriteLine("Unable to delete '{0}': {1}", TempPath, ex);
            }
        }

        public virtual async Task StartAsync(string command, string[] args, CancellationToken token)
        {
            List<string> argsList = new();

            if (!string.IsNullOrEmpty(command))
            {
                argsList.Add(command);
            }

            if (args != null)
            {
                argsList.AddRange(args);
            }

            _runner.EntrypointAssemblyPath = DotNetMonitorPath;
            _runner.Arguments = string.Join(" ", argsList);

            // Disable diagnostics on tool
            _adapter.Environment.Add("COMPlus_EnableDiagnostics", "0");
            // Console output in JSON for easy parsing
            _adapter.Environment.Add("Logging__Console__FormatterName", "json");
            // Enable Information on ASP.NET Core logs for better ability to diagnose issues.
            _adapter.Environment.Add("Logging__LogLevel__Microsoft.AspNetCore", "Information");
            // Enable Debug on Microsoft.Diagnostics to get lifetime and address events as well as for diagnosing issues.
            _adapter.Environment.Add("Logging__LogLevel__Microsoft.Diagnostics", "Debug");

            // Override the shared config directory
            _adapter.Environment.Add("DotnetMonitorTestSettings__SharedConfigDirectoryOverride", SharedConfigDirectoryPath);
            // Override the user config directory
            _adapter.Environment.Add("DotnetMonitorTestSettings__UserConfigDirectoryOverride", UserConfigDirectoryPath);

            // Set configuration via environment variables
            var configurationViaEnvironment = ConfigurationFromEnvironment.ToEnvironmentConfiguration(useDotnetMonitorPrefix: true);
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
        }

        public virtual async Task WaitForExitAsync(CancellationToken token)
        {
            await RunnerExitedTask.WithCancellation(token).ConfigureAwait(false);
            await _adapter.ReadToEnd(token).ConfigureAwait(false);
        }

        protected virtual void StandardOutputCallback(string line)
        {
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

        public async Task WriteUserSettingsAsync(RootOptions options)
        {
            using FileStream stream = new(UserSettingsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonSerializerOptionsFactory.JsonIgnoreCondition.WhenWritingNull);
            await JsonSerializer.SerializeAsync(stream, options, serializerOptions).ConfigureAwait(false);

            _outputHelper.WriteLine("Wrote user settings.");
        }

        protected void SetEnvironmentVariable(string name, string value)
        {
            _adapter.Environment[name] = value;
        }
    }
}
