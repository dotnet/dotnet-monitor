// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class EventStacksPipelineSettings : EventSourcePipelineSettings
    {
        public EventStacksPipelineSettings()
        {
            Duration = System.Threading.Timeout.InfiniteTimeSpan;
        }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    }

    internal sealed class EventStacksPipeline : EventSourcePipeline<EventStacksPipelineSettings>
    {
        private TaskCompletionSource<CallStackResult> _stackResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CallStackResult _result = new();

        public EventStacksPipeline(DiagnosticsClient client, EventStacksPipelineSettings settings)
            : base(client, settings)
        {
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(rundownKeyword: 0, bufferSizeInMB: 256, new[]
            {
                new EventPipeProvider(CallStackEvents.Provider, EventLevel.LogAlways)
            });
        }

        protected override async Task OnEventSourceAvailable(EventPipeEventSource eventSource, Func<Task> stopSessionAsync, CancellationToken token)
        {
            eventSource.Dynamic.AddCallbackForProviderEvent(CallStackEvents.Provider, eventName: null, Callback);

            using EventTaskSource<Action> sourceComplete = new EventTaskSource<Action>(
                taskComplete => taskComplete,
                addHandler => eventSource.Completed += addHandler,
                removeHandler => eventSource.Completed -= removeHandler,
                token);

            // This is the same issue as GCDumps. We don't always get events back in realtime, so we have to stop the session and then process the events.
            Task eventsTimeoutTask = Task.Delay(Settings.Timeout, token);
            Task completedTask = await Task.WhenAny(_stackResult.Task, eventsTimeoutTask);

            await completedTask;

            await stopSessionAsync();
            await sourceComplete.Task;

            if (_stackResult.Task.Status != TaskStatus.RanToCompletion)
            {
                throw new InvalidOperationException(Strings.ErrorMessage_StacksTimeout);
            }
        }

        public Task<CallStackResult> Result => _stackResult.Task;

        private void Callback(TraceEvent action)
        {
            //We do not have a manifest for our events, but we also lookup data by id instead of string.
            if (action.ID == CallStackEvents.Callstack)
            {
                var stack = new CallStack
                {
                    ThreadId = action.GetPayload<uint>(CallStackEvents.CallstackPayloads.ThreadId),
                    ThreadName = action.GetPayload<string>(CallStackEvents.CallstackPayloads.ThreadName)
                };
                ulong[] functionIds = action.GetPayload<ulong[]>(CallStackEvents.CallstackPayloads.FunctionIds);
                ulong[] offsets = action.GetPayload<ulong[]>(CallStackEvents.CallstackPayloads.IpOffsets);

                _result.Stacks.Add(stack);

                if (functionIds != null && offsets != null && functionIds.Length == offsets.Length)
                {
                    for (int i = 0; i < functionIds.Length; i++)
                    {
                        CallStackFrame stackFrame = new CallStackFrame
                        {
                            FunctionId = functionIds[i],
                            Offset = offsets[i]
                        };

                        if (_result.NameCache.FunctionData.TryGetValue(stackFrame.FunctionId, out FunctionData? functionData))
                        {
                            stackFrame.MethodToken = functionData.MethodToken;
                            if (_result.NameCache.ModuleData.TryGetValue(functionData.ModuleId, out ModuleData? moduleData))
                            {
                                stackFrame.ModuleVersionId = moduleData.ModuleVersionId;
                            }
                        }

                        stack.Frames.Add(stackFrame);
                    }
                }
            }
            else if (action.ID == CallStackEvents.FunctionDesc)
            {
                ulong id = action.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.FunctionId);
                var functionData = new FunctionData(
                    action.GetPayload<string>(NameIdentificationEvents.FunctionDescPayloads.Name),
                    action.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.MethodToken),
                    action.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ClassId),
                    action.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.ClassToken),
                    action.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ModuleId),
                    action.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.TypeArgs) ?? Array.Empty<ulong>(),
                    action.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.ParameterTypes) ?? Array.Empty<ulong>(),
                    action.GetBoolPayload(NameIdentificationEvents.FunctionDescPayloads.StackTraceHidden)
                    );

                _result.NameCache.FunctionData.TryAdd(id, functionData);
            }
            else if (action.ID == CallStackEvents.ClassDesc)
            {
                ulong id = action.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ClassId);
                var classData = new ClassData(
                    action.GetPayload<uint>(NameIdentificationEvents.ClassDescPayloads.Token),
                    action.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ModuleId),
                    (ClassFlags)action.GetPayload<uint>(NameIdentificationEvents.ClassDescPayloads.Flags),
                    action.GetPayload<ulong[]>(NameIdentificationEvents.ClassDescPayloads.TypeArgs) ?? Array.Empty<ulong>(),
                    action.GetBoolPayload(NameIdentificationEvents.ClassDescPayloads.StackTraceHidden)
                    );

                _result.NameCache.ClassData.TryAdd(id, classData);
            }
            else if (action.ID == CallStackEvents.ModuleDesc)
            {
                ulong id = action.GetPayload<ulong>(NameIdentificationEvents.ModuleDescPayloads.ModuleId);
                var moduleData = new ModuleData(
                    action.GetPayload<string>(NameIdentificationEvents.ModuleDescPayloads.Name),
                    action.GetPayload<Guid>(NameIdentificationEvents.ModuleDescPayloads.ModuleVersionId)
                    );

                _result.NameCache.ModuleData.TryAdd(id, moduleData);
            }
            else if (action.ID == CallStackEvents.TokenDesc)
            {
                ulong modId = action.GetPayload<ulong>(NameIdentificationEvents.TokenDescPayloads.ModuleId);
                uint token = action.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.Token);
                var tokenData = new TokenData(
                    action.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Name),
                    action.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Namespace),
                    action.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.OuterToken),
                    action.GetBoolPayload(NameIdentificationEvents.TokenDescPayloads.StackTraceHidden)
                    );

                _result.NameCache.TokenData.TryAdd(new ModuleScopedToken(modId, token), tokenData);
            }
            else if (action.ID == CallStackEvents.End)
            {
                //TODO Consider using opcodes instead of a separate event for stopping
                _stackResult.TrySetResult(_result);
            }
        }
    }
}
