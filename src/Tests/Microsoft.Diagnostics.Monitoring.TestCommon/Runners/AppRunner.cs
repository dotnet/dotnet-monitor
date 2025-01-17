// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    /// <summary>
    /// Runner for running the unit test application.
    /// </summary>
    [DebuggerDisplay(@"\{AppRunner:{_runner.StateForDebuggerDisplay,nq}\}")]
    public sealed class AppRunner : IAsyncDisposable
    {
        private readonly LoggingRunnerAdapter _adapter;

        private readonly string _appPath;

        private readonly string _startupHookPath;

        private readonly ITestOutputHelper _outputHelper;

        private readonly TaskCompletionSource<string> _readySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly DotNetRunner _runner = new();

        private TaskCompletionSource<object> _currentCommandSource;

        private Dictionary<string, TaskCompletionSource<string>> _waitingForEnvironmentVariables;

        private long _disposedState;

        public Architecture? Architecture
        {
            get => _runner.Architecture;
            set => _runner.Architecture = value;
        }

        public string BoundUrl { get; private set; }

        /// <summary>
        /// The mode of the diagnostic port connection. Default is <see cref="DiagnosticPortConnectionMode.Listen"/>
        /// (the application is listening for connections).
        /// </summary>
        /// <remarks>
        /// Set to <see cref="DiagnosticPortConnectionMode.Connect"/> if app needs to connect to a diagnostic port listener.
        /// </remarks>
        public DiagnosticPortConnectionMode ConnectionMode { get; set; } = DiagnosticPortConnectionMode.Listen;

        /// <summary>
        /// Path of the diagnostic port to connect to when <see cref="ConnectionMode"/> is <see cref="DiagnosticPortConnectionMode.Connect"/>.
        /// </summary>
        public string DiagnosticPortPath { get; set; }

        /// <summary>
        /// Indicates whether the process should be suspended at startup via the diagnostic port.
        /// </summary>
        /// <remarks>
        /// By default, the diagnostic port will suspend the target process.
        /// </remarks>
        public bool DiagnosticPortSuspend { get; set; } = true;

        public Dictionary<string, string> Environment => _adapter.Environment;

        public int ExitCode => _adapter.ExitCode;

        public bool HasExited => _adapter.HasExited;

        public Task<int> ProcessIdTask => _adapter.ProcessIdTask;

        /// <summary>
        /// Name of the scenario to run in the application.
        /// </summary>
        public string ScenarioName { get; set; }

        /// <summary>
        /// Optional name of the sub scenario to run in the application.
        /// </summary>
        public string SubScenarioName { get; set; }

        public int AppId { get; }

        public bool SetRuntimeIdentifier { get; set; }

        public string ProfilerLogLevel { get; set; }

        public bool EnableMonitorStartupHook { get; set; }

        public AppRunner(ITestOutputHelper outputHelper, Assembly testAssembly, int appId = 1, TargetFrameworkMoniker tfm = TargetFrameworkMoniker.Current)
        {
            AppId = appId;

            _outputHelper = new PrefixedOutputHelper(outputHelper, FormattableString.Invariant($"[App{appId}] "));

            _appPath = AssemblyHelper.GetAssemblyArtifactBinPath(
                testAssembly,
                "Microsoft.Diagnostics.Monitoring.UnitTestApp",
                tfm);

            _startupHookPath = AssemblyHelper.GetAssemblyArtifactBinPath(
                testAssembly,
                "Microsoft.Diagnostics.Monitoring.StartupHook",
                TargetFrameworkMoniker.Net60);

            _waitingForEnvironmentVariables = new Dictionary<string, TaskCompletionSource<string>>();

            _adapter = new LoggingRunnerAdapter(_outputHelper, _runner);
            _adapter.ReceivedStandardOutputLine += StandardOutputCallback;
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            _adapter.ReceivedStandardOutputLine -= StandardOutputCallback;

            await _adapter.DisposeAsync().ConfigureAwait(false);

            _readySource.TrySetCanceled();

            _runner.Dispose();
        }

        public async Task StartAsync(CancellationToken token, bool waitForReady = true)
        {
            if (string.IsNullOrEmpty(ScenarioName))
            {
                throw new InvalidOperationException($"'{nameof(ScenarioName)}' is required.");
            }

            if (!File.Exists(_appPath))
            {
                throw new FileNotFoundException($"Application path could not be found.", _appPath);
            }

            _runner.EntrypointAssemblyPath = _appPath;

            string fullScenarioName = string.IsNullOrEmpty(SubScenarioName) ? ScenarioName : string.Concat(ScenarioName, " ", SubScenarioName);
            _runner.Arguments = fullScenarioName;

            // Enable diagnostics in case it is disabled via inheriting test environment.
            _adapter.Environment.Add("DOTNET_EnableDiagnostics", "1");

            if (ConnectionMode == DiagnosticPortConnectionMode.Connect)
            {
                if (string.IsNullOrEmpty(DiagnosticPortPath))
                {
                    throw new InvalidOperationException($"'{nameof(DiagnosticPortPath)}' is required.");
                }

                string diagnosticPortsValue = DiagnosticPortPath;
                if (!DiagnosticPortSuspend)
                {
                    diagnosticPortsValue += ",nosuspend";
                }

                _adapter.Environment.Add("DOTNET_DiagnosticPorts", diagnosticPortsValue);
            }

            if (SetRuntimeIdentifier)
            {
                _adapter.Environment.Add(
                    ToolIdentifiers.EnvironmentVariables.RuntimeIdentifier,
                    NativeLibraryHelper.GetTargetRuntimeIdentifier(Architecture));
            }

            if (ProfilerLogLevel != null)
            {
                _adapter.Environment.Add(
                    ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level, ProfilerLogLevel);
            }

            if (EnableMonitorStartupHook)
            {
                _adapter.Environment.Add(ToolIdentifiers.EnvironmentVariables.StartupHooks, _startupHookPath);
            }

            await _adapter.StartAsync(token).ConfigureAwait(false);

            if (waitForReady)
            {
                await _readySource.WithCancellation(token);
            }
        }

        public Task StopAsync(CancellationToken token)
        {
            return _adapter.StopAsync(token);
        }

        public Task<int> WaitForExitAsync(CancellationToken token)
        {
            return _adapter.WaitForExitAsync(token);
        }

        private void StandardOutputCallback(string line)
        {
            try
            {
                ConsoleLogEvent logEvent = JsonSerializer.Deserialize<ConsoleLogEvent>(line);

                switch (logEvent.Category)
                {
                    case "Microsoft.Diagnostics.Monitoring.UnitTestApp.Program":
                        HandleProgramEvent(logEvent);
                        break;
                }
            }
            catch (JsonException)
            {
                // Unable to parse the output. These could be lines written to stdout that are not JSON formatted.
                // For example, asking dotnet to create a dump will write a message to stdout that is not JSON formatted.
                _outputHelper.WriteLine("Unable to JSON parse stdout line: {0}", line);
            }
        }

        public Task EndScenarioAsync(CancellationToken token)
        {
            return SendCommandAsync(TestAppScenarios.Commands.EndScenario, token);
        }

        public async Task SendCommandAsync(string command, CancellationToken token)
        {
            Assert.Null(_currentCommandSource);
            _currentCommandSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _runner.StandardInput.WriteLine(command);

            await _currentCommandSource.WithCancellation(token).ConfigureAwait(false);
            _currentCommandSource = null;
        }

        public Task StartScenarioAsync(CancellationToken token)
        {
            return SendCommandAsync(TestAppScenarios.Commands.StartScenario, token);
        }

        private void HandleProgramEvent(ConsoleLogEvent logEvent)
        {
            switch ((TestAppLogEventIds)logEvent.EventId)
            {
                case TestAppLogEventIds.ScenarioState:
                    Assert.True(logEvent.State.TryGetValue("State", out TestAppScenarios.ScenarioState state));
                    switch (state)
                    {
                        case TestAppScenarios.ScenarioState.Ready:
                            Assert.True(_readySource.TrySetResult(null));
                            break;
                    }
                    break;
                case TestAppLogEventIds.ReceivedCommand:
                    Assert.NotNull(_currentCommandSource);
                    Assert.True(logEvent.State.TryGetValue("Expected", out bool expected));
                    Assert.True(logEvent.State.TryGetValue("Command", out _));
                    if (expected)
                    {
                        Assert.True(_currentCommandSource.TrySetResult(null));
                    }
                    else
                    {
                        Assert.True(_currentCommandSource.TrySetException(new InvalidOperationException(logEvent.Message)));
                    }
                    break;
                case TestAppLogEventIds.EnvironmentVariable:
                    Assert.True(logEvent.State.TryGetValue("Name", out string name));
                    Assert.True(logEvent.State.TryGetValue("Value", out string value));
                    lock (_waitingForEnvironmentVariables)
                    {
                        if (_waitingForEnvironmentVariables.TryGetValue(name, out TaskCompletionSource<string> completedGettingVal))
                        {
                            _outputHelper.WriteLine($"Processing callback for envVar: {name}");
                            Assert.True(completedGettingVal.TrySetResult(value));
                            _waitingForEnvironmentVariables.Remove(name);
                        }
                    }
                    break;
                case TestAppLogEventIds.BoundUrl:
                    Assert.True(logEvent.State.TryGetValue("Url", out string url));
                    BoundUrl = url;
                    break;
            }
        }

        public Task<string> WaitForEnvironmentVariable(string name, CancellationToken token)
        {
            TaskCompletionSource<string> waiter;
            lock (_waitingForEnvironmentVariables)
            {
                if (_waitingForEnvironmentVariables.ContainsKey(name))
                {
                    throw new InvalidOperationException();
                }
                _waitingForEnvironmentVariables.Add(name, new TaskCompletionSource<string>());
                waiter = _waitingForEnvironmentVariables[name];
            }

            return waiter.WithCancellation(token);
        }
    }
}
