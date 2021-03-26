// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public sealed class LoggingRunnerAdapter : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly ITestOutputHelper _outputHelper;
        private readonly DotNetRunner _runner;
        private readonly List<string> _standardErrorLines = new();
        private readonly List<string> _standardOutputLines = new();

        private bool _isDiposed;
        private Task _standardErrorTask;
        private Task _standardOutputTask;

        public Dictionary<string, string> Environment { get; } = new();

        public Action<string> StandardErrorCallback { get; set; }

        public Action<string> StandardOutputCallback { get; set; }

        public LoggingRunnerAdapter(ITestOutputHelper outputHelper, DotNetRunner runner)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async ValueTask DisposeAsync()
        {
            lock (_cancellation)
            {
                if (_isDiposed)
                {
                    return;
                }
                _isDiposed = true;
            }

            _cancellation.Cancel();

            // Shutdown the runner
            _runner.ForceClose();

            // Wait for it to exit
            await WaitForExitAsync(CancellationToken.None).SafeAwait(_outputHelper, -1).ConfigureAwait(false);

            await _standardErrorTask.SafeAwait(_outputHelper).ConfigureAwait(false);
            await _standardOutputTask.SafeAwait(_outputHelper).ConfigureAwait(false);

            _outputHelper.WriteLine("Begin Standard Output:");
            foreach (string line in _standardOutputLines)
            {
                _outputHelper.WriteLine(line);
            }
            _outputHelper.WriteLine("End Standard Output:");

            _outputHelper.WriteLine("Begin Standard Error:");
            foreach (string line in _standardErrorLines)
            {
                _outputHelper.WriteLine(line);
            }
            _outputHelper.WriteLine("End Standard Error:");

            _cancellation.Dispose();
        }

        public async Task StartAsync(CancellationToken token)
        {
            _outputHelper.WriteLine("Path: {0}", _runner.EntrypointAssemblyPath);
            _outputHelper.WriteLine("Args: {0}", _runner.Arguments);

            _outputHelper.WriteLine("Begin Environment:");
            foreach (KeyValuePair<string, string> variable in Environment)
            {
                _outputHelper.WriteLine("- {0} = {1}", variable.Key, variable.Value);
                _runner.Environment[variable.Key] = variable.Value;
            }
            _outputHelper.WriteLine("End Environment:");

            _outputHelper.WriteLine("Starting...");
            await _runner.StartAsync(token).ConfigureAwait(false);
            _outputHelper.WriteLine("Process ID: {0}", _runner.ProcessId);

            _standardErrorTask = ReadLinesAsync(_runner.StandardError, _standardErrorLines, StandardErrorCallback, _cancellation.Token);
            _standardOutputTask = ReadLinesAsync(_runner.StandardOutput, _standardOutputLines, StandardOutputCallback, _cancellation.Token);
        }

        public async Task<int> WaitForExitAsync(CancellationToken token)
        {
            if (!_runner.HasExited)
            {
                _outputHelper.WriteLine("Waiting for exit...");
                await _runner.WaitForExitAsync(token).ConfigureAwait(false);
                _outputHelper.WriteLine("Exit Code: {0}", _runner.ExitCode);
            }
            return _runner.ExitCode;
        }

        private static async Task ReadLinesAsync(StreamReader reader, List<string> lines, Action<string> callback, CancellationToken token)
        {
            try
            {
                await foreach (string line in reader.ReadLinesAsync(token))
                {
                    lines.Add(line);
                    callback?.Invoke(line);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
