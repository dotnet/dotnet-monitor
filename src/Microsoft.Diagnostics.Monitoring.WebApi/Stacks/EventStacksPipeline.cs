// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return new EventPipeProviderSourceConfiguration(requestRundown: false, bufferSizeInMB: 256, new[]
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
                        stack.Frames.Add(new CallStackFrame { FunctionId = functionIds[i], Offset = offsets[i] });
                    }
                }
            }
            else if (action.ID == CallStackEvents.FunctionDesc)
            {
                ulong id = action.GetPayload<ulong>(CallStackEvents.FunctionDescPayloads.FunctionId);
                var functionData = new FunctionData
                {
                    Name = action.GetPayload<string>(CallStackEvents.FunctionDescPayloads.Name),
                    ParentClass = action.GetPayload<ulong>(CallStackEvents.FunctionDescPayloads.ClassId),
                    ParentToken = action.GetPayload<uint>(CallStackEvents.FunctionDescPayloads.ClassToken),
                    ModuleId = action.GetPayload<ulong>(CallStackEvents.FunctionDescPayloads.ModuleId),
                    TypeArgs = action.GetPayload<ulong[]>(CallStackEvents.FunctionDescPayloads.TypeArgs) ?? Array.Empty<ulong>()
                };

                _result.NameCache.FunctionData.Add(id, functionData);
            }
            else if (action.ID == CallStackEvents.ClassDesc)
            {
                ulong id = action.GetPayload<ulong>(CallStackEvents.ClassDescPayloads.ClassId);
                var classData = new ClassData
                {
                    ModuleId = action.GetPayload<ulong>(CallStackEvents.ClassDescPayloads.ModuleId),
                    Token = action.GetPayload<uint>(CallStackEvents.ClassDescPayloads.Token),
                    Flags = (ClassFlags)action.GetPayload<uint>(CallStackEvents.ClassDescPayloads.Flags),
                    TypeArgs = action.GetPayload<ulong[]>(CallStackEvents.ClassDescPayloads.TypeArgs) ?? Array.Empty<ulong>()
                };

                _result.NameCache.ClassData.Add(id, classData);
            }
            else if (action.ID == CallStackEvents.ModuleDesc)
            {
                ulong id = action.GetPayload<ulong>(CallStackEvents.ModuleDescPayloads.ModuleId);
                var moduleData = new ModuleData
                {
                    Name = action.GetPayload<string>(CallStackEvents.ModuleDescPayloads.Name)
                };

                _result.NameCache.ModuleData.Add(id, moduleData);
            }
            else if (action.ID == CallStackEvents.TokenDesc)
            {
                ulong modId = action.GetPayload<ulong>(CallStackEvents.TokenDescPayloads.ModuleId);
                ulong token = action.GetPayload<uint>(CallStackEvents.TokenDescPayloads.Token);
                var tokenData = new TokenData()
                {
                    Name = action.GetPayload<string>(CallStackEvents.TokenDescPayloads.Name),
                    OuterToken = action.GetPayload<uint>(CallStackEvents.TokenDescPayloads.OuterToken)
                };

                _result.NameCache.TokenData.Add((modId, token), tokenData);
            }
            else if (action.ID == CallStackEvents.End)
            {
                //TODO Consider using opcodes instead of a separate event for stopping
                _stackResult.TrySetResult(_result);
            }
        }
    }
}
