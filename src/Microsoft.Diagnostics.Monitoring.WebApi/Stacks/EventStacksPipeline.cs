// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
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
        private TaskCompletionSource<StackResult> _stackResult = new();
        private StackResult _result = new();

        public EventStacksPipeline(DiagnosticsClient client, EventStacksPipelineSettings settings)
            : base(client, settings)
        {
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(requestRundown: false, bufferSizeInMB: 256, new[]
            {
                new EventPipeProvider(StackEvents.Provider, System.Diagnostics.Tracing.EventLevel.LogAlways)
            });
        }

        protected override async Task OnEventSourceAvailable(EventPipeEventSource eventSource, Func<Task> stopSessionAsync, CancellationToken token)
        {
            eventSource.Dynamic.AddCallbackForProviderEvents((string provider, string _) => provider == StackEvents.Provider ?
            EventFilterResponse.AcceptEvent : EventFilterResponse.RejectEvent, Callback);

            using EventTaskSource<Action> sourceComplete = new EventTaskSource<Action>(
                taskComplete => taskComplete,
                addHandler => eventSource.Completed += addHandler,
                removeHandler => eventSource.Completed -= removeHandler,
                token);

            // This is the same issue as GCDumps. We don't always get events back in realtime, so we have to stop the session and then process the events.
            Task eventsTimeoutTask = Task.Delay(Settings.Timeout, token);
            Task completedTask = await Task.WhenAny(_stackResult.Task, eventsTimeoutTask);

            token.ThrowIfCancellationRequested();

            await stopSessionAsync();
            await sourceComplete.Task;

            if (_stackResult.Task.Status != TaskStatus.RanToCompletion)
            {
                throw new InvalidOperationException("Unable to process stack in timely manner.");
            }
        }

        public Task<StackResult> Result => _stackResult.Task;

        private void Callback(TraceEvent action)
        {
            if (action.ProviderName != StackEvents.Provider)
            {
                return;
            }

            //We do not have a manifest for our events, but we also lookup data by id instead of string.
            if (action.ID == StackEvents.Callstack)
            {
                var stack = new Stack
                {
                    ThreadId = action.GetPayload<uint>(StackEvents.CallstackPayloads.ThreadId)
                };
                ulong[] functionIds = action.GetPayload<ulong[]>(StackEvents.CallstackPayloads.FunctionIds);
                ulong[] offsets = action.GetPayload<ulong[]>(StackEvents.CallstackPayloads.IpOffsets);

                _result.Stacks.Add(stack);

                if (functionIds != null && offsets != null)
                {
                    for (int i = 0; i < functionIds.Length; i++)
                    {
                        stack.Frames.Add(new StackFrame { FunctionId = functionIds[i], Offset = offsets[i] });
                    }
                }
            }
            else if (action.ID == StackEvents.FunctionDesc)
            {
                ulong id = action.GetPayload<ulong>(StackEvents.FunctionDescPayloads.FunctionId);
                var functionData = new FunctionData
                {
                    Name = action.GetPayload<string>(StackEvents.FunctionDescPayloads.Name),
                    ParentClass = action.GetPayload<ulong>(StackEvents.FunctionDescPayloads.ClassId),
                    ParentToken = action.GetPayload<uint>(StackEvents.FunctionDescPayloads.ClassToken),
                    ModuleId = action.GetPayload<ulong>(StackEvents.FunctionDescPayloads.ModuleId),
                    TypeArgs = action.GetPayload<ulong[]>(StackEvents.FunctionDescPayloads.TypeArgs) ?? Array.Empty<ulong>()
                };

                _result.NameCache.FunctionData.Add(id, functionData);
            }
            else if (action.ID == StackEvents.ClassDesc)
            {
                ulong id = action.GetPayload<ulong>(StackEvents.ClassDescPayloads.ClassId);
                var classData = new ClassData
                {
                    ModuleId = action.GetPayload<ulong>(StackEvents.ClassDescPayloads.ModuleId),
                    Token = action.GetPayload<uint>(StackEvents.ClassDescPayloads.Token),
                    Flags = (ClassFlags)action.GetPayload<uint>(StackEvents.ClassDescPayloads.Flags),
                    TypeArgs = action.GetPayload<ulong[]>(StackEvents.ClassDescPayloads.TypeArgs) ?? Array.Empty<ulong>()
                };

                _result.NameCache.ClassData.Add(id, classData);
            }
            else if (action.ID == StackEvents.ModuleDesc)
            {
                ulong id = action.GetPayload<ulong>(StackEvents.ModuleDescPayloads.ModuleId);
                var moduleData = new ModuleData
                {
                    Name = action.GetPayload<string>(StackEvents.ModuleDescPayloads.Name)
                };

                _result.NameCache.ModuleData.Add(id, moduleData);
            }
            else if (action.ID == StackEvents.TokenDesc)
            {
                ulong modId = action.GetPayload<ulong>(StackEvents.TokenDescPayloads.ModuleId);
                ulong token = action.GetPayload<uint>(StackEvents.TokenDescPayloads.Token);
                TokenData tokenData = new()
                {
                    Name = action.GetPayload<string>(StackEvents.TokenDescPayloads.Name),
                    OuterToken = action.GetPayload<uint>(StackEvents.TokenDescPayloads.OuterToken)
                };

                _result.NameCache.TokenData.Add((modId, token), tokenData);
            }
            else if (action.ID == StackEvents.End)
            {
                //TODO Consider using opcodes instead of a separate event for stopping
                _stackResult.TrySetResult(_result);
            }
        }
    }
}
