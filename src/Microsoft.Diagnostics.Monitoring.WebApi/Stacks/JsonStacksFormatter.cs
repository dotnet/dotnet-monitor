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
            var builder = new StringBuilder();

            foreach (CallStack stack in stackResult.Stacks)
            {
                Models.CallStack stackModel = new Models.CallStack();
                stackModel.ThreadId = stack.ThreadId;
                stackModel.ThreadName = stack.ThreadName;

                foreach (CallStackFrame frame in stack.Frames)
                {
                    Models.CallStackFrame frameModel = new Models.CallStackFrame()
                    {
                        ClassName = NameFormatter.UnknownClass,
                        MethodName = UnknownFunction,
                        //TODO Bring this back once we have a useful offset value
                        //Offset = frame.Offset,
                        ModuleName = NameFormatter.UnknownModule
                    };
                    if (frame.FunctionId == 0)
                    {
                        frameModel.MethodName = NativeFrame;
                        frameModel.ModuleName = NativeFrame;
                        frameModel.ClassName = NativeFrame;
                    }
                    else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
                    {
                        frameModel.ModuleName = NameFormatter.GetModuleName(cache, functionData.ModuleId);
                        frameModel.MethodName = functionData.Name;

                        builder.Clear();
                        NameFormatter.BuildClassName(builder, cache, functionData);
                        frameModel.ClassName = builder.ToString();

                        if (functionData.TypeArgs.Length > 0)
                        {
                            builder.Clear();
                            builder.Append(functionData.Name);
                            NameFormatter.BuildGenericParameters(builder, cache, functionData.TypeArgs);
                            frameModel.MethodName = builder.ToString();
                        }
                    }

                    stackModel.Frames.Add(frameModel);
                }
                stackResultModel.Stacks.Add(stackModel);
            }

            await JsonSerializer.SerializeAsync(OutputStream, stackResultModel, cancellationToken: token);
        }
    }
}
