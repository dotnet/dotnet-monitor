// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal sealed class CapturedParametersTextFormatter(Stream outputStream) : CapturedParametersFormatter(outputStream)
    {
        private const string Indent = "  ";

        private readonly StreamWriter _writer = new(outputStream, Encoding.UTF8, leaveOpen: true);

        protected override async Task WriteCapturedMethodAsync(CapturedMethod capture, CancellationToken token)
        {
            StringBuilder builder = new();

            builder.AppendLine(FormattableString.Invariant($"[{capture.CapturedDateTime}][thread {capture.ThreadId}] {GetValueOrUnknown(capture.ActivityId)}[format: {capture.ActivityIdFormat}]"));
            builder.AppendLine(FormattableString.Invariant($"{Indent}{GetValueOrUnknown(capture.ModuleName)}!{GetValueOrUnknown(capture.TypeName)}.{GetValueOrUnknown(capture.MethodName)}("));

            foreach (CapturedParameter parameter in capture.Parameters)
            {
                builder.Append(FormattableString.Invariant($"{Indent}{Indent}{GetValueOrUnknown(parameter.TypeModuleName)}!{GetValueOrUnknown(parameter.Type)} {GetValueOrUnknown(parameter.Name)}: "));
                string paramValue = parameter.EvalFailReason switch
                {
                    EvaluationFailureReason.None => parameter.Value ?? "null",
                    EvaluationFailureReason.HasSideEffects => "<evaluation has side effects>",
                    EvaluationFailureReason.NotSupported => "<evaluation not supported>",
                    _ => "<unknown evaluation failure>",
                };
                builder.AppendLine(paramValue);
            }

            builder.AppendLine(FormattableString.Invariant($"{Indent})"));

            await _writer.WriteAsync(builder, token);
        }

        protected override Task WriteItemSeparatorAsync(CancellationToken token) => _writer.WriteLineAsync();

        protected override Task WritePrologAsync(CancellationToken token) => _writer.WriteLineAsync("Captured Parameters:");

        protected override async ValueTask DisposeInternalAsync()
        {
            await _writer.FlushAsync();
            await _writer.DisposeAsync();
        }

        private static string GetValueOrUnknown(string? value) => string.IsNullOrEmpty(value) ? "<unknown>" : value;
    }
}
