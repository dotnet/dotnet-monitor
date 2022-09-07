// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public override async Task FormatStack(StackResult stackResult, CancellationToken token)
        {
            Models.StackResult result = new Models.StackResult();
            NameCache cache = stackResult.NameCache;

            foreach(Stack stack in stackResult.Stacks)
            {
                Models.Stack stackModel = new Models.Stack();
                stackModel.ThreadId = stack.ThreadId;

                foreach(StackFrame frame in stack.Frames)
                {
                    Models.StackFrame frameModel = new Models.StackFrame()
                    {
                        ClassName = UnknownClass,
                        MethodName = UnknownFunction,
                        Offset = frame.Offset,
                        ModuleName = UnknownModule
                    };
                    if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
                    {
                        frameModel.ModuleName = GetModuleName(cache, functionData.ModuleId);
                        frameModel.MethodName = functionData.Name;

                        StringBuilder builder = new StringBuilder();
                        if (functionData.ParentClass != 0)
                        {
                            BuildClassName(builder, cache, functionData.ParentClass);
                        }
                        else
                        {
                            BuildClassName(builder, cache, functionData.ModuleId, functionData.ParentToken);
                        }
                        frameModel.ClassName = builder.ToString();
                        builder.Clear();

                        if (functionData.TypeArgs.Length > 0)
                        {
                            builder.Append(functionData.Name);
                            BuildGenericParameters(builder, cache, functionData.TypeArgs);
                            frameModel.MethodName = builder.ToString();
                        }
                    }

                    stackModel.Frames.Add(frameModel);
                }
                result.Stacks.Add(stackModel);
            }

            await JsonSerializer.SerializeAsync(OutputStream, result);
        }
    }
}
