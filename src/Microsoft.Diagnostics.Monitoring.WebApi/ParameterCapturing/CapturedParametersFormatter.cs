// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal abstract class CapturedParametersFormatter : IAsyncDisposable
    {
        private bool _isFirstItem = true;

        public CapturedParametersFormatter(Stream outputStream)
        {
            OutputStream = outputStream;
        }

        public Task StartAsync(CancellationToken cancellationToken) => WritePrologAsync(cancellationToken);

        public async Task WriteParameters(ICapturedParameters parameters, CancellationToken token)
        {
            CapturedMethod capturedMethod = BuildResultModel(parameters);

            if (!_isFirstItem)
            {
                await WriteItemSeparatorAsync(token);
            }
            else
            {
                _isFirstItem = false;
            }

            await WriteCapturedMethodAsync(capturedMethod, token);
        }

        public ValueTask DisposeAsync() => DisposeInternalAsync();

        protected Stream OutputStream { get; }

        protected abstract Task WritePrologAsync(CancellationToken token);

        protected abstract Task WriteCapturedMethodAsync(CapturedMethod capturedMethod, CancellationToken token);

        protected abstract Task WriteItemSeparatorAsync(CancellationToken token);

        protected abstract ValueTask DisposeInternalAsync();

        private static CapturedMethod BuildResultModel(ICapturedParameters capture) => new CapturedMethod()
        {
            ActivityId = capture.ActivityId,
            ActivityIdFormat = capture.ActivityIdFormat,
            ThreadId = capture.ThreadId,
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
                EvalFailReason = param.EvalFailReason
            }).ToList()
        };
    }
}
