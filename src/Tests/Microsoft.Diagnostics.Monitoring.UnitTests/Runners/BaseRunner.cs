// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    /// <summary>
    /// Base class for common runner logic.
    /// </summary>
    internal abstract class BaseRunner : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();

        // Cancellation registration used to unregister that cancellation callback
        // that cancels the TaskCompletionSource<T> fields.
        private readonly IDisposable _cancellationRegistration;

        private readonly bool _enableDiagnostics;

        private readonly string _entrypointAssemblyPath;

        private bool _isDisposed;

        private readonly string _logPrefix;

        private readonly ITestOutputHelper _outputHelper;

        private readonly DotNetRunner _runner = new();

        // Completion source signaled when the process is running
        private readonly TaskCompletionSource<object> _startedSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly List<string> _stdOutLines = new();

        public int ProcessId => _runner.ProcessId;

        protected StreamWriter StandardInput => _runner.StandardInput;

        private List<Task> _joinableTasks = new();

        protected BaseRunner(RunnerOptions options)
        {
            _enableDiagnostics = options.EnableDiagnostics;
            _entrypointAssemblyPath = options.EntrypointAssemblyPath ?? throw new ArgumentNullException(nameof(options.EntrypointAssemblyPath));
            _logPrefix = options.LogPrefix ?? throw new ArgumentNullException(nameof(options.LogPrefix));
            _outputHelper = options.OutputHelper ?? throw new ArgumentNullException(nameof(options.OutputHelper));

            _runner.WaitForDiagnosticPipe = options.WaitForDiagnosticPipe;

            CancellationToken token = _cancellation.Token;
            _cancellationRegistration = token.Register(() => OnCancel(token));
        }

        /// <summary>
        /// Starts the process.
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            List<string> argsList = new(GetProcessArguments());
            string args = string.Join(" ", argsList);

            LogLine("Path: {0}", _entrypointAssemblyPath);
            LogLine("Args: {0}", args);

            _runner.EntryAssemblyPath = _entrypointAssemblyPath;
            _runner.Arguments = args;

            IDictionary<string, string> environment = GetProcessEnvironment();
            environment.Add("COMPlus_EnableDiagnostics", _enableDiagnostics ? "1" : "0");

            foreach (var variable in environment)
            {
                _runner.SetEnvironmentVariable(variable.Key, variable.Value);
            }

            // Start running dotnet-monitor
            await _runner.StartAsync(token).ConfigureAwait(false);

            LogLine("Process ID: {0}", _runner.ProcessId);

            // Cancel the completion sources if cancellation is requested
            using var _ = token.Register(() => OnCancel(token));

            _joinableTasks.Add(ProcessStandardOutputAsync(_cancellation.Token));

            await _startedSource.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the process with a timeout.
        /// </summary>
        public async Task StartAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            await StartAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Shuts down the process.
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

            OutputHelperShim outputHelper = new(this);

            // Wait for it to exit
            int exitCode = await _runner.WaitForExitAsync(CancellationToken.None).SafeAwait(outputHelper, -1).ConfigureAwait(false);
            LogLine("Exit Code: {0}", exitCode);

            // Cancel any remaining tasks
            _cancellation.Cancel();

            OnCancel(_cancellation.Token);

            // Wait for stdout processing to finish
            await Task.WhenAll(_joinableTasks).SafeAwait(outputHelper).ConfigureAwait(false);

            LogLine("Begin Standard Output");
            foreach (string line in _stdOutLines)
            {
                LogLine(line);
            }
            LogLine("End Standard Output");

            OnDispose();

            // Dispose cancellation registrations
            _cancellationRegistration.Dispose();

            _cancellation.Dispose();
        }

        protected void LogLine(string message)
        {
            _outputHelper.WriteLine($"{_logPrefix} {message}");
        }

        protected void LogLine(string format, params object[] args)
        {
            _outputHelper.WriteLine($"{_logPrefix} {format}", args);
        }

        protected bool TrySetStarted()
        {
            return _startedSource.TrySetResult(null);
        }

        protected static async Task<T> GetCompletionSourceResultAsync<T>(TaskCompletionSource<T> source, CancellationToken token)
        {
            TaskCompletionSource<T> cancellationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using var _ = token.Register(() => cancellationSource.TrySetCanceled(token));

            Task<T> completedTask = await Task.WhenAny(
                source.Task,
                cancellationSource.Task).ConfigureAwait(false);

            return await completedTask.ConfigureAwait(false);
        }

        protected virtual IDictionary<string, string> GetProcessEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected virtual IEnumerable<string> GetProcessArguments()
        {
            return Enumerable.Empty<string>();
        }

        protected virtual void OnCancel(CancellationToken token)
        {
            _startedSource.TrySetCanceled(token);
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual void OnStandardOutputLine(string line)
        {
        }

        private async Task ProcessStandardOutputAsync(CancellationToken token)
        {
            try
            {
                await foreach (string line in _runner.StandardOutput.ReadLinesAsync(token))
                {
                    OnStandardOutputLine(line);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // All log events have this structure (plus additional fields
        // not needed by the test runner for identifying events).
        protected class LogEvent
        {
            public string Category { get; set; }

            public int EventId { get; set; }

            public string Message { get; set; }

            public Dictionary<string, JsonElement> State { get; set; }
        }

        protected class RunnerOptions
        {
            public bool EnableDiagnostics { get; set; }

            public string EntrypointAssemblyPath { get; set; }

            public string LogPrefix { get; set; }

            public ITestOutputHelper OutputHelper { get; set; }

            public bool WaitForDiagnosticPipe { get; set; }
        }

        private class OutputHelperShim : ITestOutputHelper
        {
            private readonly BaseRunner _runner;

            public OutputHelperShim(BaseRunner runner)
            {
                _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            }

            public void WriteLine(string message) => _runner.LogLine(message);

            public void WriteLine(string format, params object[] args) => _runner.LogLine(format, args);
        }
    }
}
