// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class JsonStacksFormatter : StacksFormatter
    {
        public JsonStacksFormatter(Stream outputStream) : base(outputStream)
        {
        }

        public override async Task FormatStack(CallStackResult stackResult, CancellationToken token)
        {
            Models.CallStackResult stackResultModel = new Models.CallStackResult();
            NameCache cache = stackResult.NameCache;

            foreach (CallStack stack in stackResult.Stacks)
            {
                var stackModel = StackUtilities.TranslateCallStackToModel(stack, cache);

                var builder = new StringBuilder();

                foreach (var frameModel in stackModel.Frames)
                {
                    builder.Clear();
                    builder.Append(frameModel.MethodName);
                    builder.Append(frameModel.TypeArgs);

                    frameModel.MethodName = builder.ToString();
                }

                stackResultModel.Stacks.Add(stackModel);
            }

            await JsonSerializer.SerializeAsync(OutputStream, stackResultModel, cancellationToken: token);
        }
    }
}
