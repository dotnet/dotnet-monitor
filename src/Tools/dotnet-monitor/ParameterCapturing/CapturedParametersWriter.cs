// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturedParametersWriter : IAsyncDisposable
    {
        private readonly Channel<ICapturedParameters> _parameters = Channel.CreateUnbounded<ICapturedParameters>();
        private readonly CancellationToken _cancellationToken;
        private readonly Task _writerTask;
        private readonly CapturedParametersFormatter _formatter;

        public CapturedParametersWriter(Stream outputStream, CapturedParameterFormat format, CancellationToken token)
        {
            _cancellationToken = token;
            _formatter = format switch
            {
                CapturedParameterFormat.PlainText => new CapturedParametersTextFormatter(outputStream),
                CapturedParameterFormat.JsonSequence => new CapturedParametersJsonFormatter(outputStream, format),
                CapturedParameterFormat.NewlineDelimitedJson => new CapturedParametersJsonFormatter(outputStream, format),
                _ => throw new ArgumentException(nameof(format)),
            };
            _writerTask = WriterLoop();
        }

        public void AddCapturedParameters(ICapturedParameters capturedParameters)
        {
            _parameters.Writer.TryWrite(capturedParameters);
        }

        public async ValueTask DisposeAsync()
        {
            _parameters.Writer.Complete();
            await _writerTask;
            await _formatter.DisposeAsync();
        }

        public Task WaitAsync() => _writerTask;

        private async Task WriterLoop()
        {
            await _formatter.StartAsync(_cancellationToken);

            while (await _parameters.Reader.WaitToReadAsync(_cancellationToken))
            {
                if (_parameters.Reader.TryRead(out ICapturedParameters? parameter))
                {
                    await _formatter.WriteParameters(parameter, _cancellationToken);
                }
            }
        }
    }
}
