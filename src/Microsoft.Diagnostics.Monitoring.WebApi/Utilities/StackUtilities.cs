// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal enum StackFormat
    {
        Json,
        PlainText,
        Speedscope
    }

    internal static class StackUtilities
    {
        public static Models.CallStack TranslateCallStackToModel(CallStack stack, NameCache cache)
        {
            Models.CallStack stackModel = new Models.CallStack();
            stackModel.ThreadId = stack.ThreadId;
            stackModel.ThreadName = stack.ThreadName;

            foreach (CallStackFrame frame in stack.Frames)
            {
                stackModel.Frames.Add(CreateFrameModel(frame, cache));
            }

            return stackModel;
        }

        internal static Models.CallStackFrame CreateFrameModel(CallStackFrame frame, NameCache cache)
        {
            var builder = new StringBuilder();

            Models.CallStackFrame frameModel = new Models.CallStackFrame()
            {
                ClassName = NameFormatter.UnknownClass,
                MethodName = StacksFormatter.UnknownFunction,
                //TODO Bring this back once we have a useful offset value
                //Offset = frame.Offset,
                ModuleName = NameFormatter.UnknownModule
            };
            if (frame.FunctionId == 0)
            {
                frameModel.MethodName = StacksFormatter.NativeFrame;
                frameModel.ModuleName = StacksFormatter.NativeFrame;
                frameModel.ClassName = StacksFormatter.NativeFrame;
            }
            else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
            {
                frameModel.ModuleName = NameFormatter.GetModuleName(cache, functionData.ModuleId);

                builder.Clear();
                builder.Append(functionData.Name);

                if (functionData.TypeArgs.Length > 0)
                {
                    NameFormatter.BuildGenericParameters(builder, cache, functionData.TypeArgs);
                }

                frameModel.MethodName = builder.ToString();

                if (functionData.ParameterTypes.Length > 0)
                {
                    builder.Clear();
                    frameModel.ParameterTypes = NameFormatter.GetMethodParameterTypes(builder, cache, functionData.ParameterTypes);
                }

                builder.Clear();
                NameFormatter.BuildClassName(builder, cache, functionData);
                frameModel.ClassName = builder.ToString();
            }

            return frameModel;
        }

        internal static StacksFormatter CreateFormatter(StackFormat format, Stream outputStream) =>
            format switch
            {
                StackFormat.Json => new JsonStacksFormatter(outputStream),
                StackFormat.Speedscope => new SpeedscopeStacksFormatter(outputStream),
                StackFormat.PlainText => new TextStacksFormatter(outputStream),
                _ => throw new InvalidOperationException(),
            };
    }
}
