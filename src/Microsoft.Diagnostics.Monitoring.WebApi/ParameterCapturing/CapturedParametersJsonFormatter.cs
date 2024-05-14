// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal sealed class CapturedParametersJsonFormatter : CapturedParametersFormatter
    {
        private static readonly byte[] NewLineDelimiter = [(byte)'\n'];
        private static readonly byte[] JsonSequenceRecordSeparator = [0x1E];

        private readonly CapturedParameterFormat _format;

        public CapturedParametersJsonFormatter(Stream outputStream, CapturedParameterFormat format) : base(outputStream)
        {
            if (format is not CapturedParameterFormat.JsonSequence and not CapturedParameterFormat.NewlineDelimitedJson)
            {
                throw new ArgumentException(FormattableString.Invariant($"The {nameof(format)} value has to be a JSON variant."));
            }
            _format = format;
        }

        protected override async Task WriteCapturedMethodAsync(CapturedMethod capturedMethod, CancellationToken token)
        {
            if (_format == CapturedParameterFormat.JsonSequence)
            {
                await OutputStream.WriteAsync(JsonSequenceRecordSeparator, token);
            }

            await JsonSerializer.SerializeAsync(OutputStream, capturedMethod, cancellationToken: token);
        }

        protected override async Task WriteItemSeparatorAsync(CancellationToken token)
        {
            await OutputStream.WriteAsync(NewLineDelimiter, token);
        }

        protected override Task WritePrologAsync(CancellationToken token) => Task.CompletedTask;

        protected override ValueTask DisposeInternalAsync() => ValueTask.CompletedTask;
    }
}
