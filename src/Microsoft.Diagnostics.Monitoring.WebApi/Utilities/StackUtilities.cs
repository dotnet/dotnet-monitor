// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            foreach (CallStackFrame frame in stack.Frames)
            {
                stackModel.Frames.Add(CreateFrameModel(frame, cache, methodNameIncludesGenericParameters));
            }

            return stackModel;
        }

        internal static Models.CallStackFrame CreateFrameModel(CallStackFrame frame, NameCache cache, bool methodNameIncludesGenericParameters)
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
                    if (methodNameIncludesGenericParameters)
                    {
                        NameFormatter.BuildGenericParameters(builder, cache, functionData.TypeArgs, TypeFormat.FullName);
                        frameModel.MethodName = builder.ToString();
                    }
                    else
                    {
                        frameModel.MethodName = builder.ToString();

                        builder.Clear();
                        frameModel.GenericParameterTypes = NameFormatter.GetTypes(builder, cache, functionData.TypeArgs, TypeFormat.FullName);
                        builder.Clear();
                        frameModel.GenericParameterFullTypes = NameFormatter.GetTypes(builder, cache, functionData.TypeArgs, TypeFormat.OmitNamespace);
                    }
                }
                else
                {
                    frameModel.MethodName = builder.ToString();
                }

                if (functionData.ParameterTypes.Length > 0)
                {
                    builder.Clear();
                    frameModel.ParameterTypes = NameFormatter.GetTypes(builder, cache, functionData.ParameterTypes, TypeFormat.FullName);
                    builder.Clear();
                    frameModel.ParameterFullTypes = NameFormatter.GetTypes(builder, cache, functionData.ParameterTypes, TypeFormat.OmitNamespace);
                }

                builder.Clear();
                NameFormatter.BuildClassName(builder, cache, functionData);
                frameModel.ClassName = builder.ToString();
            }

            return frameModel;
        }

        public static string GenerateStacksFilename(IEndpointInfo endpointInfo, bool plainText)
        {
            string extension = plainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.stacks.{extension}");
        }

        public static async Task CollectStacksAsync(TaskCompletionSource<object> startCompletionSource,
            IEndpointInfo endpointInfo,
            ProfilerChannel profilerChannel,
            StackFormat format,
            Stream outputStream, CancellationToken token)
        {
            var settings = new EventStacksPipelineSettings
            {
                Duration = Timeout.InfiniteTimeSpan
            };
            await using var eventTracePipeline = new EventStacksPipeline(new DiagnosticsClient(endpointInfo.Endpoint), settings);

            Task runPipelineTask = await eventTracePipeline.StartAsync(token);

            //CONSIDER Should we set this before or after the profiler message has been sent.
            startCompletionSource?.TrySetResult(null);

            ProfilerMessage response = await profilerChannel.SendMessage(
                endpointInfo,
                new ProfilerMessage { MessageType = ProfilerMessageType.Callstack, Parameter = 0 },
                token);

            if (response.MessageType == ProfilerMessageType.Error)
            {
                throw new InvalidOperationException($"Profiler request failed: 0x{response.Parameter:X8}");
            }
            await runPipelineTask;
            Stacks.CallStackResult result = await eventTracePipeline.Result;

            StacksFormatter formatter = CreateFormatter(format, outputStream);

            await formatter.FormatStack(result, token);
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
