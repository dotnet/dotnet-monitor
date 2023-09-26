// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    public sealed class LoggingRunnerAdapter : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly ITestOutputHelper _outputHelper;
        private readonly TaskCompletionSource<int> _processIdSource =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly DotNetRunner _runner;
        private readonly List<string> _standardErrorLines = new();
        private readonly List<string> _standardOutputLines = new();

        private bool _finishReads;
        private long _disposedState;
        private Task _standardErrorTask;
        private Task _standardOutputTask;

        public Dictionary<string, string> Environment { get; } = new();
        public int ExitCode => _runner.HasExited ?
            _runner.ExitCode : throw new InvalidOperationException("Must call WaitForExitAsync before getting exit code.");

        public bool HasExited => _runner.HasExited;

        public Task<int> ProcessIdTask => _processIdSource.Task;

        public event Action<string> ReceivedStandardErrorLine;

        public event Action<string> ReceivedStandardOutputLine;

        public LoggingRunnerAdapter(ITestOutputHelper outputHelper, DotNetRunner runner)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            _cancellation.SafeCancel();

            _processIdSource.TrySetCanceled(_cancellation.Token);

            // Shutdown the runner
            await StopAsync(CancellationToken.None).SafeAwait(_outputHelper).ConfigureAwait(false);

            // Wait for it to exit
            await WaitForExitAsync(CancellationToken.None).SafeAwait(_outputHelper, -1).ConfigureAwait(false);

            await _standardErrorTask.SafeAwait(_outputHelper).ConfigureAwait(false);
            await _standardOutputTask.SafeAwait(_outputHelper).ConfigureAwait(false);

            _outputHelper.WriteLine("Begin Standard Output:");
            foreach (string line in _standardOutputLines)
            {
                _outputHelper.WriteLine(line);
            }
            _outputHelper.WriteLine("End Standard Output");

            _outputHelper.WriteLine("Begin Standard Error:");
            foreach (string line in _standardErrorLines)
            {
                _outputHelper.WriteLine(line);
            }
            _outputHelper.WriteLine("End Standard Error");

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

            using (var _ = token.Register(() => _processIdSource.TrySetCanceled(token)))
            {
                _outputHelper.WriteLine("Starting...");
                await _runner.StartAsync(token).ConfigureAwait(false);
            }

            _outputHelper.WriteLine("Process ID: {0}", _runner.ProcessId);
            _processIdSource.TrySetResult(_runner.ProcessId);

            _standardErrorTask = ReadLinesAsync(_runner.StandardError, _standardErrorLines, ReceivedStandardErrorLine, _cancellation.Token);
            _standardOutputTask = ReadLinesAsync(_runner.StandardOutput, _standardOutputLines, ReceivedStandardOutputLine, _cancellation.Token);
        }

        public async Task StopAsync(CancellationToken token)
        {
            if (_runner.HasExited)
            {
                _outputHelper.WriteLine("Already stopped.");
            }
            else
            {
                _outputHelper.WriteLine("Stopping...");

                await _runner.StopAsync(token);
            }
        }

        public async Task<int> WaitForExitAsync(CancellationToken token)
        {
            int? exitCode;
            if (!_runner.HasStarted)
            {
                _outputHelper.WriteLine("Runner Never Started.");
                throw new InvalidOperationException("The runner has never been started, call StartAsync first.");
            }
            else if (_runner.HasExited)
            {
                _outputHelper.WriteLine("Already exited.");
                exitCode = _runner.ExitCode;
            }
            else
            {
                _outputHelper.WriteLine("Waiting for exit...");
                await _runner.WaitForExitAsync(token).ConfigureAwait(false);
                exitCode = _runner.ExitCode;
            }
            _outputHelper.WriteLine("Exit Code: {0}", exitCode);
            return exitCode.Value;
        }

        public async Task ReadToEnd(CancellationToken token)
        {
            // First we need to wait for the process to end
            await WaitForExitAsync(token);

            // Then tell the readers to end by setting _finishReads and grab the waiter tasks
            _finishReads = true;
            Task errorWaiter = _standardErrorTask.SafeAwait(_outputHelper);
            Task stdWaiter = _standardOutputTask.SafeAwait(_outputHelper);

            // Wait for everything to end with the cancellation token still allowed to abort the wait
            await Task.WhenAll(stdWaiter, errorWaiter).WithCancellation(token).ConfigureAwait(false);
        }

        private async Task ReadLinesAsync(StreamReader reader, List<string> lines, Action<string> callback, CancellationToken cancelToken)
        {
#if !NET7_0_OR_GREATER
            // Closing the reader to cancel the async await will dispose the underlying stream.
            // Technically, this means the reader/stream cannot be used after canceling reading of lines
            // from the process, but this is probably okay since the adapter is already logging each line
            // and providing a callback to callers to read each line. It's unlikely the reader/stream will
            // be accessed after this adapter is disposed.
            using var cancelReg = cancelToken.Register(reader.Close);
#endif

            try
            {
                bool readAborted = false;
                while (!_finishReads)
                {
#if NET7_0_OR_GREATER
                    string line = await reader.ReadLineAsync(cancelToken).ConfigureAwait(false);
#else
                    // ReadLineAsync does not have cancellation in 6.0 or lower
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
#endif

                    if (null == line)
                    {
                        readAborted = true;
                        break;
                    }

                    lines.Add(line);
                    callback?.Invoke(line);
                }

                // If the loop ended because _finishReads was set, we should read to the end of the
                // stream if readAborted is not set. This is so we can ensure that the entire stream is read.
                if (!readAborted && _finishReads)
                {
#if NET7_0_OR_GREATER
                    string remainder = await reader.ReadToEndAsync(cancelToken).ConfigureAwait(false);
#else
                    // ReadToEndAsync does not have cancellation in 6.0 or lower
                    string remainder = await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
                    foreach (string line in remainder.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        lines.Add(line);
                        callback?.Invoke(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
