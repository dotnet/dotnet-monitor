﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    /// <summary>
    /// Runner for running the unit test application.
    /// </summary>
    internal sealed class AppRunner : IAsyncDisposable
    {
        private readonly LoggingRunnerAdapter _adapter;

        private readonly ITestOutputHelper _outputHelper;

        private readonly TaskCompletionSource<string> _readySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly DotNetRunner _runner = new();

        private TaskCompletionSource<object> _currentCommandSource;

        private bool _isDiposed;

        /// <summary>
        /// The path of the currently executing assembly.
        /// </summary>
        private static string CurrentExecutingAssemblyPath =>
            Assembly.GetExecutingAssembly().Location;

        /// <summary>
        /// The path to the application.
        /// </summary>
        private static string AppPath =>
            CurrentExecutingAssemblyPath
                .Replace(Assembly.GetExecutingAssembly().GetName().Name, "Microsoft.Diagnostics.Monitoring.UnitTestApp");

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

        public Dictionary<string, string> Environment => _adapter.Environment;

        public int ExitCode => _adapter.ExitCode;

        public int ProcessId => _adapter.ProcessId;

        /// <summary>
        /// Name of the scenario to run in the application.
        /// </summary>
        public string ScenarioName { get; set; }

        public AppRunner(ITestOutputHelper outputHelper, int appId = 1)
        {
            _outputHelper = new PrefixedOutputHelper(outputHelper, FormattableString.Invariant($"[App{appId}] "));

            _adapter = new LoggingRunnerAdapter(_outputHelper, _runner);
            _adapter.ReceivedStandardOutputLine += StandardOutputCallback;
        }

        public async ValueTask DisposeAsync()
        {
            lock (_adapter)
            {
                if (_isDiposed)
                {
                    return;
                }
                _isDiposed = true;
            }

            _adapter.ReceivedStandardOutputLine -= StandardOutputCallback;

            await _adapter.DisposeAsync().ConfigureAwait(false);

            CancelCompletionSources(CancellationToken.None);

            _runner.Dispose();
        }

        public async Task StartAsync(CancellationToken token)
        {
            if (string.IsNullOrEmpty(ScenarioName))
            {
                throw new ArgumentNullException(nameof(ScenarioName));
            }

            _runner.EntrypointAssemblyPath = AppPath;
            _runner.Arguments = ScenarioName;

            // Enable diagnostics in case it is disabled via inheriting test environment.
            _adapter.Environment.Add("COMPlus_EnableDiagnostics", "1");

            if (ConnectionMode == DiagnosticPortConnectionMode.Connect)
            {
                if (string.IsNullOrEmpty(DiagnosticPortPath))
                {
                    throw new ArgumentNullException(nameof(DiagnosticPortPath));
                }

                _adapter.Environment.Add("DOTNET_DiagnosticPorts", DiagnosticPortPath);
            }

            await _adapter.StartAsync(token).ConfigureAwait(false);

            using IDisposable _ = token.Register(() => CancelCompletionSources(token));

            await _readySource.Task;
        }

        public Task<int> WaitForExitAsync(CancellationToken token)
        {
            return _adapter.WaitForExitAsync(token);
        }

        private void CancelCompletionSources(CancellationToken token)
        {
            _readySource.TrySetCanceled(token);
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
                // Unable to parse the output. These could be lines writen to stdout that are not JSON formatted.
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

            await _currentCommandSource.GetAsync(token).ConfigureAwait(false);

            _currentCommandSource = null;
        }

        public Task StartScenarioAsync(CancellationToken token)
        {
            return SendCommandAsync(TestAppScenarios.Commands.StartScenario, token);
        }

        private void HandleProgramEvent(ConsoleLogEvent logEvent)
        {
            switch (logEvent.EventId)
            {
                case 1: // ScenarioState
                    Assert.True(logEvent.State.TryGetValue("state", out TestAppScenarios.SenarioState state));
                    switch (state)
                    {
                        case TestAppScenarios.SenarioState.Ready:
                            Assert.True(_readySource.TrySetResult(null));
                            break;
                    }
                    break;
                case 2: // ReceivedCommand
                    Assert.NotNull(_currentCommandSource);
                    Assert.True(logEvent.State.TryGetValue("expected", out bool expected));
                    if (expected)
                    {
                        Assert.True(_currentCommandSource.TrySetResult(null));
                    }
                    else
                    {
                        Assert.True(_currentCommandSource.TrySetException(new InvalidOperationException(logEvent.Message)));
                    }
                    break;
            }
        }
    }
}
