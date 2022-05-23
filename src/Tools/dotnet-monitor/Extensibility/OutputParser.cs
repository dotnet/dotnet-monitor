// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class OutputParser<TResult> : IDisposable where TResult : class
    {
        private readonly ILogger<ProgramExtension> _logger;
        private readonly TaskCompletionSource<TResult> _resultCompletionSource;
        private readonly Process _process;

        public OutputParser(Process process, ILogger<ProgramExtension> logger)
        {
            _process = process;
            _logger = logger;
            _resultCompletionSource = new TaskCompletionSource<TResult>();

            _process.OutputDataReceived += ParseStdOut;
            _process.ErrorDataReceived += ParseErrOut;

            _process.Exited += ProcExited;
        }


        public void Dispose()
        {
            _process.OutputDataReceived -= ParseStdOut;
            _process.ErrorDataReceived -= ParseErrOut;
            _process.Exited -= ProcExited;
        }

        public Task<TResult> ReadResult()
        {
            return _resultCompletionSource.Task;
        }

        private void ParseStdOut(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
            {
                try
                {
                    // Check if the object is a TResult
                    TResult result = JsonSerializer.Deserialize<TResult>(eventArgs.Data);
                    _resultCompletionSource.TrySetResult(result);
                }
                catch (Exception)
                {
                    // Expected that some things won't parse correctly
                }
                _logger.ExtensionOutputMessage(_process.Id, eventArgs.Data);
            }
        }

        private void ParseErrOut(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
            {
                _logger.ExtensionErrorMessage(_process.Id, eventArgs.Data);
            }
        }

        private void ProcExited(object sender, EventArgs e)
        {
            _resultCompletionSource.TrySetResult(null);
        }
    }
}
