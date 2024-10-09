// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    [DebuggerDisplay(@"\{MonitorRunner:{_runner.StateForDebuggerDisplay,nq}\}")]
    internal class MonitorRunner : IAsyncDisposable
    {
        private const string TestHostingStartupAssemblyName = "Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup";
        private const string TestStartupHookAssemblyName = "Microsoft.Diagnostics.Monitoring.Tool.TestStartupHook";

        protected readonly object _lock = new();

        protected readonly ITestOutputHelper _outputHelper;

        private readonly DotNetRunner _runner = new();

        private readonly LoggingRunnerAdapter _adapter;

        private readonly TemporaryDirectory _tempDir;

        private long _disposedState;

        private bool _useExplicitlySetConfiguration;

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
        /// Gets the standard input of the dotnet-monitor process
        /// </summary>
        public StreamWriter StandardInput => _runner.StandardInput;

        public bool HasExited => _runner.HasExited;

        public int ExitCode => _runner.ExitCode;

        /// <summary>
        /// The path to dotnet-monitor.
        /// </summary>
        private static string DotNetMonitorPath =>
            AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                "dotnet-monitor",
                TargetFrameworkMoniker.Net80
                );

        private static string TestStartupHookPath =>
            AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                TestStartupHookAssemblyName,
                TargetFrameworkMoniker.Net80
                );

        private string SharedConfigDirectoryPath =>
            Path.Combine(TempPath, "SharedConfig");

        private string UserConfigDirectoryPath =>
            Path.Combine(TempPath, "UserConfig");

        private string UserSettingsFilePath =>
            Path.Combine(UserConfigDirectoryPath, "settings.json");

        private string ExplicitlySetSettingsFilePath =>
            Path.Combine(TempPath, "settings.json");

        public string TempPath => _tempDir.FullName;

        public MonitorRunner(ITestOutputHelper outputHelper)
        {
            _outputHelper = new PrefixedOutputHelper(outputHelper, "[Monitor] ");

            _tempDir = new TemporaryDirectory(_outputHelper);

            _adapter = new LoggingRunnerAdapter(_outputHelper, _runner);
            _adapter.ReceivedStandardOutputLine += StandardOutputCallback;

            Directory.CreateDirectory(SharedConfigDirectoryPath);
            Directory.CreateDirectory(UserConfigDirectoryPath);
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (!TestCommon.DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            _adapter.ReceivedStandardOutputLine -= StandardOutputCallback;
            await _adapter.DisposeAsync().ConfigureAwait(false);

            _runner.Dispose();

            _tempDir.Dispose();
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

            if (_useExplicitlySetConfiguration)
            {
                argsList.Add("--configuration-file-path");
                argsList.Add($"\"{ExplicitlySetSettingsFilePath}\"");
                _outputHelper.WriteLine("Explicitly set settings path: {0}", ExplicitlySetSettingsFilePath);
            }

            _runner.EntrypointAssemblyPath = DotNetMonitorPath;
            _runner.Arguments = string.Join(" ", argsList);

            // Disable diagnostics on tool
            _adapter.Environment.Add("DOTNET_EnableDiagnostics", "0");
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

            // Ensures that the TestStartupHook is loaded early so it helps resolve other test assemblies
            _adapter.Environment.Add(ToolIdentifiers.EnvironmentVariables.StartupHooks, TestStartupHookPath);

            // Allow TestHostingStartup to participate in host building in the tool
            _adapter.Environment.Add("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", TestHostingStartupAssemblyName);

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

        public Task StopAsync(CancellationToken token)
        {
            return _adapter.StopAsync(token);
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
            await WriteSettingsFileAsync(options, UserSettingsFilePath).ConfigureAwait(false);
            _outputHelper.WriteLine("Wrote user settings.");
        }

        public async Task WriteExplicitlySetSettingsFileAsync(RootOptions options)
        {
            await WriteSettingsFileAsync(options, ExplicitlySetSettingsFilePath).ConfigureAwait(false);
            _useExplicitlySetConfiguration = true;
            _outputHelper.WriteLine("Wrote settings file.");
        }

        protected void SetEnvironmentVariable(string name, string value)
        {
            _adapter.Environment[name] = value;
        }

        private static async Task WriteSettingsFileAsync(RootOptions options, string filePath)
        {
            using FileStream stream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonIgnoreCondition.WhenWritingNull);
            await JsonSerializer.SerializeAsync(stream, options, serializerOptions).ConfigureAwait(false);
        }
    }
}
