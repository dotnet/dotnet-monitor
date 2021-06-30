﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected readonly LoggingRunnerAdapter _adapter;

        protected readonly ITestOutputHelper _outputHelper;

        protected readonly DotNetRunner _runner = new();

        protected readonly string _runnerTmpPath =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));

        private bool _isDisposed;

        /// <summary>
        /// The path of the currently executing assembly.
        /// </summary>
        protected static string CurrentExecutingAssemblyPath =>
            Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// The target framework name of the currently executing assembly.
        /// </summary>
        protected static string CurrentTargetFrameworkFolderName =>
            new FileInfo(CurrentExecutingAssemblyPath).Directory.Name;

        /// <summary>
        /// The path to dotnet-monitor. It is currently only build for the
        /// netcoreapp3.1 target framework.
        /// </summary>
        protected static string DotNetMonitorPath =>
            CurrentExecutingAssemblyPath
                .Replace(Assembly.GetExecutingAssembly().GetName().Name, "dotnet-monitor")
                .Replace(CurrentTargetFrameworkFolderName, "netcoreapp3.1");

        /// <summary>
        /// Sets configuration values via environment variables.
        /// </summary>
        public RootOptions ConfigurationFromEnvironment { get; } = new();

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

        public virtual async ValueTask DisposeAsync()
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
            // Enable Debug on Startup class to get lifetime and address events
            _adapter.Environment.Add("Logging__LogLevel__Microsoft.Diagnostics.Tools.Monitor.Startup", "Debug");

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
            await Task.Run(() => _runner.ExitedTask.Wait(token)).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }
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

        public async Task WriteUserSettingsAsync(RootOptions options, CancellationToken token)
        {
            using FileStream stream = new(UserSettingsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonSerializerOptionsFactory.JsonIgnoreCondition.WhenWritingNull);
            await JsonSerializer.SerializeAsync(stream, options, serializerOptions).ConfigureAwait(false);

            _outputHelper.WriteLine("Wrote user settings.");
        }

        public async Task WriteUserSettingsAsync(RootOptions options, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await WriteUserSettingsAsync(options, cancellation.Token).ConfigureAwait(false);
        }
    }
}
