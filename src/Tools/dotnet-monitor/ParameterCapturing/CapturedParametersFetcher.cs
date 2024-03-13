// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Models = Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturedParametersFetcher : IArtifactOperation
    {
        private readonly IEndpointInfo _endpointInfo;
        private readonly IParameterCapturingStore _store;
        private readonly CapturedParameterFormat _format;
        private readonly Guid? _requestId;

        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CapturedParametersFetcher(IEndpointInfo endpointInfo, Guid? requestId, CapturedParameterFormat format)
        {
            _endpointInfo = endpointInfo;
            _format = format;
            _requestId = requestId;
            _store = endpointInfo.ServiceProvider.GetRequiredService<IParameterCapturingStore>();
        }

        public string ContentType => _format switch
        {
            CapturedParameterFormat.PlainText => ContentTypes.TextPlain,
            CapturedParameterFormat.Json => ContentTypes.ApplicationJson,
            _ => ContentTypes.TextPlain
        };

        public bool IsStoppable => false;

        public Task Started => _startCompletionSource.Task;

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            _startCompletionSource.TrySetResult();

            IReadOnlyList<ICapturedParameters> capturedParameters = _store.GetCapturedParameters();
            switch (_format)
            {
                case CapturedParameterFormat.PlainText:
                    await WriteParametersAsPlainText(capturedParameters, _requestId, outputStream, token);
                    break;
                case CapturedParameterFormat.Json:
                    await WriteParametersAsJson(capturedParameters, _requestId, outputStream, token);
                    break;
                default: throw new InvalidOperationException();
            }
        }

        public string GenerateFileName()
        {
            string extension = _format == CapturedParameterFormat.PlainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{_endpointInfo.ProcessId}.parameters.{extension}");
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        private static Task WriteParametersAsJson(IEnumerable<ICapturedParameters> capturedParameters, Guid? requestId, Stream outputStream, CancellationToken token)
        {
            Models.CapturedParametersResult result = BuildResultModel(capturedParameters, requestId);

            return JsonSerializer.SerializeAsync(outputStream, result, cancellationToken: token);
        }

        private const string Indent = "  ";

        private static async Task WriteParametersAsPlainText(IEnumerable<ICapturedParameters> capturedParameters, Guid? requestId, Stream outputStream, CancellationToken token)
        {
            Models.CapturedParametersResult result = BuildResultModel(capturedParameters, requestId);

            await using StreamWriter writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("Captured Parameters:");

            StringBuilder builder = new();

            foreach (Models.CapturedMethod capture in result.CapturedMethods)
            {
                builder.AppendLine($"[{capture.CapturedDateTime}][{capture.RequestId}] {GetValueOrUnknown(capture.ActivityId)}");
                builder.AppendLine($"{Indent}{GetValueOrUnknown(capture.ModuleName)}!{GetValueOrUnknown(capture.MethodName)}.{GetValueOrUnknown(capture.MethodName)}(");

                foreach (Models.CapturedParameter parameter in capture.Parameters)
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

        private static string GetValueOrUnknown(string value) => string.IsNullOrEmpty(value) ? " <unknown>" : value;

        private static Models.CapturedParametersResult BuildResultModel(IEnumerable<ICapturedParameters> capturedParameters, Guid? requestId)
        {
            IEnumerable<ICapturedParameters> filteredResults = capturedParameters;
            if (requestId is not null)
            {
                filteredResults = capturedParameters.Where(c => c.RequestId == requestId);
            }

            return new Models.CapturedParametersResult
            {
                CapturedMethods = filteredResults.Select(capture => new Models.CapturedMethod()
                {
                    RequestId = capture.RequestId,
                    ActivityId = capture.ActivityId,
                    CapturedDateTime = capture.CapturedDateTime,
                    ModuleName = capture.ModuleName,
                    TypeName = capture.TypeName,
                    MethodName = capture.MethodName,
                    Parameters = capture.Parameters.Select(param => new Models.CapturedParameter()
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
