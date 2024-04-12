// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal sealed class CapturedParametersJsonFormatter: CapturedParametersFormatter
    {
        private static readonly byte[] NewLineDelimiter = [(byte)'\n'];
        private static readonly byte[] ArrayItemDelimiter = [(byte)','];
        private static readonly byte[] ArrayStartToken = [(byte)'['];
        private static readonly byte[] ArrayEndToken = [(byte)']'];

        private readonly bool _writeNdJson;

        public CapturedParametersJsonFormatter(Stream outputStream, bool writeNdJson) : base(outputStream)
        {
            _writeNdJson = writeNdJson;
        }

        protected override Task WriteCapturedMethodAsync(CapturedMethod capturedMethod, CancellationToken token) =>
            JsonSerializer.SerializeAsync(OutputStream, capturedMethod, cancellationToken: token);

        protected override async Task WriteItemSeparatorAsync(CancellationToken token)
        {
            if (_writeNdJson)
            {
                await OutputStream.WriteAsync(NewLineDelimiter, token);
            }
            else
            {
                await OutputStream.WriteAsync(ArrayItemDelimiter, token);
            }
        }

        protected override async Task WritePrologAsync(CancellationToken token)
        {
            if (!_writeNdJson)
            {
                await OutputStream.WriteAsync(ArrayStartToken, token);
            }
        }

        protected override async ValueTask DisposeInternalAsync()
        {
            if (!_writeNdJson)
            {
                await OutputStream.WriteAsync(ArrayEndToken);
            }
        }
    }
}
