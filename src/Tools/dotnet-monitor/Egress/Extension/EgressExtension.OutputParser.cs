// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal partial class EgressExtension
    {
        internal class OutputParser<TResult> : IDisposable where TResult : class, IExtensionResult
        {
            private readonly ILogger<EgressExtension> _logger;
            private readonly TaskCompletionSource<TResult> _resultCompletionSource;
            private readonly EventWaitHandle _beginReadsHandle;
            private readonly Process _process;
            // We need to store the process ID for logging because we can't access it after the process exits
            private int _processId = -1;

            public OutputParser(Process process, ILogger<EgressExtension> logger)
            {
                _process = process;
                _logger = logger;
                _resultCompletionSource = new TaskCompletionSource<TResult>();
                _beginReadsHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                _process.OutputDataReceived += ParseStdOut;
                _process.ErrorDataReceived += ParseErrOut;

                _process.Exited += ProcExited;
            }

            public void BeginReading()
            {
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                _beginReadsHandle.Set();
                _processId = _process.Id;
            }

            public void Dispose()
            {
                // We don't own _process, so don't dispose it, but do unregister the handlers

                _process.OutputDataReceived -= ParseStdOut;
                _process.ErrorDataReceived -= ParseErrOut;
                _process.Exited -= ProcExited;

                _beginReadsHandle.Dispose();
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
                            _resultCompletionSource.TrySetResult(result);
                        }
                        else
                        {
                            _logger.ExtensionMalformedOutput(_processId, eventArgs.Data, typeof(TResult));
                        }
                    }
                    catch (JsonException)
                    {
                        // Expected that some things won't parse correctly
                    }
                    _logger.ExtensionOutputMessage(_processId, eventArgs.Data);
                }
            }

            private void ParseErrOut(object sender, DataReceivedEventArgs eventArgs)
            {
                if (eventArgs.Data != null)
                {
                    _logger.ExtensionErrorMessage(_processId, eventArgs.Data);
                }
            }

            private void ProcExited(object sender, EventArgs e)
            {
                // We need to make sure we started reading in-order to be sure
                // that output streams will be processed
                _beginReadsHandle.WaitOne();

                _resultCompletionSource.TrySetResult(null);
            }
        }
    }
}
