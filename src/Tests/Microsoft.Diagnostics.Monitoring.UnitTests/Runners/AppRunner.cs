// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class AppRunner : BaseRunner
    {
        private TaskCompletionSource<object> _currentCommandSource;

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

        /// <summary>
        /// Name of the scenario to run in the application.
        /// </summary>
        public string ScenarioName { get; set; }

        public AppRunner(ITestOutputHelper outputHelper, int appId = 1)
            : base(CreateRunnerOptions(outputHelper, appId))
        {
        }

        protected override IEnumerable<string> GetProcessArguments()
        {
            List<string> argsList = new();

            argsList.Add(ScenarioName);

            return argsList;
        }

        protected override IDictionary<string, string> GetProcessEnvironment()
        {
            IDictionary<string, string> environment = base.GetProcessEnvironment();

            if (ConnectionMode == DiagnosticPortConnectionMode.Connect)
            {
                if (string.IsNullOrEmpty(DiagnosticPortPath))
                {
                    throw new ArgumentNullException(nameof(DiagnosticPortPath));
                }

                environment.Add("DOTNET_DiagnosticPorts", DiagnosticPortPath);
            }

            return environment;
        }

        protected override void OnStandardOutputLine(string line)
        {
            LogEvent logEvent = JsonSerializer.Deserialize<LogEvent>(line);

            switch (logEvent.Category)
            {
                case "Microsoft.Diagnostics.Monitoring.UnitTestApp.Program":
                    HandleProgramEvent(logEvent);
                    break;
            }
        }

        public Task EndScenarioAsync(CancellationToken token)
        {
            return SendCommandAsync(TestAppScenarios.Commands.EndScenario, token);
        }

        public async Task SendEndScenarioAsync(TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await EndScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        public async Task SendCommandAsync(string command, CancellationToken token)
        {
            Assert.Null(_currentCommandSource);
            _currentCommandSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            StandardInput.WriteLine(command);

            await GetCompletionSourceResultAsync(_currentCommandSource, token).ConfigureAwait(false);

            _currentCommandSource = null;
        }

        public async Task SendCommandAsync(string command, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await SendCommandAsync(command, cancellation.Token).ConfigureAwait(false);
        }

        public Task StartScenarioAsync(CancellationToken token)
        {
            return SendCommandAsync(TestAppScenarios.Commands.StartScenario, token);
        }

        public async Task SendStartScenarioAsync(TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await StartScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        private static RunnerOptions CreateRunnerOptions(ITestOutputHelper outputHelper, int appId)
        {
            if (null == outputHelper)
            {
                throw new ArgumentNullException(nameof(outputHelper));
            }

            return new RunnerOptions()
            {
                EnableDiagnostics = true,
                EntrypointAssemblyPath = AppPath,
                LogPrefix = FormattableString.Invariant($"App{appId}"),
                OutputHelper = outputHelper,
                WaitForDiagnosticPipe = true
            };
        }

        private void HandleProgramEvent(LogEvent logEvent)
        {
            switch (logEvent.EventId)
            {
                case 1: // ScenarioReady
                    Assert.True(TrySetStarted());
                    break;
                case 5: // ReceivedCommand
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
