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
    internal partial class ProgramExtension
    {
        internal class OutputParser<TResult> : IDisposable where TResult : class, IExtensionResult
        {
            private readonly ILogger<ProgramExtension> _logger;
            private readonly object _lock = new object();
            private readonly TaskCompletionSource<TResult> _resultCompletionSource;
            private readonly Process _process;
            private bool _resultReceived = false;

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
                        if (result.IsValid())
                        {
                            lock (_lock)
                            {
                                _resultCompletionSource.TrySetResult(result);
                                _resultReceived = true;
                            }
                        }
                        else
                        {
                            _logger.ExtensionMalformedOutput(_process.Id, eventArgs.Data, typeof(TResult));
                        }
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
                lock (_lock)
                {
                    if (!_resultReceived)
                    {
                        _resultCompletionSource.TrySetResult(null);
                    }
                }
            }
        }
    }
}
