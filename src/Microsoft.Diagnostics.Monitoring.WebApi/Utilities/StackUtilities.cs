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
        public static Models.CallStack TranslateCallStackToModel(CallStack stack, NameCache cache, bool methodNameIncludesGenericParameters = true)
        {
            Models.CallStack stackModel = new Models.CallStack();
            stackModel.ThreadId = stack.ThreadId;
            stackModel.ThreadName = stack.ThreadName;

            StringBuilder builder = new();
            foreach (CallStackFrame frame in stack.Frames)
            {
                var frameModel = CreateFrameModel(frame, cache);
                if (methodNameIncludesGenericParameters)
                {
                    builder.Append(frameModel.MethodName);
                    NameFormatter.BuildGenericArgTypes(builder, frameModel.FullGenericArgTypes);
                    frameModel.MethodName = builder.ToString();
                    builder.Clear();
                }
                stackModel.Frames.Add(frameModel);
            }

            return stackModel;
        }

        internal static Models.CallStackFrame CreateFrameModel(CallStackFrame frame, NameCache cache)
        {
            var builder = new StringBuilder();

            Models.CallStackFrame frameModel = new Models.CallStackFrame()
            {
                TypeName = NameFormatter.UnknownClass,
                MethodName = StacksFormatter.UnknownFunction,
                MethodToken = 0,
                //TODO Bring this back once we have a useful offset value
                //Offset = frame.Offset,
                ModuleName = NameFormatter.UnknownModule,
                ModuleVersionId = Guid.Empty
            };
            if (frame.FunctionId == 0)
            {
                frameModel.MethodName = StacksFormatter.NativeFrame;
                frameModel.ModuleName = StacksFormatter.NativeFrame;
                frameModel.TypeName = StacksFormatter.NativeFrame;
            }
            else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData? functionData))
            {
                frameModel.MethodToken = functionData.MethodToken;
                frameModel.ModuleName = NameFormatter.GetModuleName(cache, functionData.ModuleId);

                if (cache.ModuleData.TryGetValue(functionData.ModuleId, out ModuleData? moduleData))
                {
                    frameModel.ModuleVersionId = moduleData.ModuleVersionId;
                }

                builder.Clear();
                builder.Append(functionData.Name);

                frameModel.MethodName = builder.ToString();

                if (functionData.TypeArgs.Length > 0)
                {
                    frameModel.SimpleGenericArgTypes = NameFormatter.GetTypeNames(cache, functionData.TypeArgs, NameFormatter.TypeFormat.Simple);
                    frameModel.FullGenericArgTypes = NameFormatter.GetTypeNames(cache, functionData.TypeArgs, NameFormatter.TypeFormat.Full);
                }

                if (functionData.ParameterTypes.Length > 0)
                {
                    frameModel.SimpleParameterTypes = NameFormatter.GetTypeNames(cache, functionData.ParameterTypes, NameFormatter.TypeFormat.Simple);
                    frameModel.FullParameterTypes = NameFormatter.GetTypeNames(cache, functionData.ParameterTypes, NameFormatter.TypeFormat.Full);
                }

                builder.Clear();
                NameFormatter.BuildTypeName(builder, cache, functionData);
                frameModel.TypeName = builder.ToString();
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
