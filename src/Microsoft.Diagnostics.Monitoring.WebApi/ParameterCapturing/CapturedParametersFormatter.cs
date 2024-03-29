// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal static class CapturedParametersFormatter
    {
        public static Task WriteAsync(
            IEnumerable<ICapturedParameters> parameters,
            CapturedParameterFormat format,
            Stream outputStream,
            CancellationToken token)
        {
            return format switch
            {
                CapturedParameterFormat.PlainText => WriteParametersAsPlainText(parameters, outputStream, token),
                CapturedParameterFormat.Json => WriteParametersAsJson(parameters, outputStream, token),
                _ => throw new InvalidOperationException(),
            };
        }

        private static Task WriteParametersAsJson(IEnumerable<ICapturedParameters> parameters, Stream outputStream, CancellationToken token)
        {
            CapturedParametersResult result = BuildResultModel(parameters);

            return JsonSerializer.SerializeAsync(outputStream, result, cancellationToken: token);
        }

        private const string Indent = "  ";

        private static async Task WriteParametersAsPlainText(IEnumerable<ICapturedParameters> parameters, Stream outputStream, CancellationToken token)
        {
            CapturedParametersResult result = BuildResultModel(parameters);

            await using StreamWriter writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("Captured Parameters:");

            StringBuilder builder = new();

            foreach (CapturedMethod capture in result.CapturedMethods)
            {
                builder.AppendLine($"[{capture.CapturedDateTime}][{capture.RequestId}] {GetValueOrUnknown(capture.ActivityId)}");
                builder.AppendLine($"{Indent}{GetValueOrUnknown(capture.ModuleName)}!{GetValueOrUnknown(capture.TypeName)}.{GetValueOrUnknown(capture.MethodName)}(");

                foreach (CapturedParameter parameter in capture.Parameters)
                {
                    builder.Append($"{Indent}{Indent}{GetValueOrUnknown(parameter.TypeModuleName)}!{GetValueOrUnknown(parameter.Type)} ");
                    if (parameter.IsInParameter)
                    {
                        builder.Append("in ");
                    }
                    else if (parameter.IsOutParameter)
                    {
                        builder.Append("out ");
                    }
                    else if (parameter.IsByRefParameter)
                    {
                        builder.Append("ref ");
                    }
                    builder.AppendLine($"{GetValueOrUnknown(parameter.Name)}: {GetValueOrUnknown(parameter.Value)}");
                }

                builder.AppendLine($"{Indent})");
                builder.AppendLine();
            }

            await writer.WriteAsync(builder, token);

#if NET8_0_OR_GREATER
            await writer.FlushAsync(token);
#else
            await writer.FlushAsync();
#endif
        }

        private static string GetValueOrUnknown(string value) => string.IsNullOrEmpty(value) ? "<unknown>" : value;

        private static CapturedParametersResult BuildResultModel(IEnumerable<ICapturedParameters> parameters)
        {
            return new CapturedParametersResult
            {
                CapturedMethods = parameters.Select(capture => new CapturedMethod()
                {
                    RequestId = capture.RequestId,
                    ActivityId = capture.ActivityId,
                    CapturedDateTime = capture.CapturedDateTime,
                    ModuleName = capture.ModuleName,
                    TypeName = capture.TypeName,
                    MethodName = capture.MethodName,
                    Parameters = capture.Parameters.Select(param => new CapturedParameter()
                    {
                        Name = param.Name,
                        Type = param.Type,
                        TypeModuleName = param.TypeModuleName,
                        Value = param.Value,
                        IsInParameter = param.IsIn,
                        IsOutParameter = param.IsOut,
                        IsByRefParameter = param.IsByRef
                    }).ToList()
                }).ToList()
            };
        }
    }
}
