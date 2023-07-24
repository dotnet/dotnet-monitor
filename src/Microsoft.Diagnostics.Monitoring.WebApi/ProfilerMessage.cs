// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if STARTUPHOOK || HOSTINGSTARTUP
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
#else
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
#endif
using System;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring
{
    internal sealed class EmptyPayload { }

    internal sealed class StartCapturingParametersPayload
    {
        public Guid RequestId { get; set; } = Guid.Empty;
        public TimeSpan Duration { get; set; } = Timeout.InfiniteTimeSpan;
        public MethodDescription[] Methods { get; set; } = Array.Empty<MethodDescription>();
    }

    internal sealed class StopCapturingParametersPayload
    {
        public Guid RequestId { get; set; } = Guid.Empty;
    }

    internal enum IpcCommand : short
    {
        Unknown,
        Status,
        Callstack,
        StartCapturingParameters,
        StopCapturingParameters
    };

    internal interface IProfilerMessage
    {
        public IpcCommand Command { get; }
        public byte[] Payload { get; }
    }

    internal struct JsonProfilerMessage : IProfilerMessage
    {
        public IpcCommand Command { get; }
        public byte[] Payload { get; }

        public JsonProfilerMessage(IpcCommand command, object payloadObject)
        {
            Command = command;
            Payload = SerializePayload(payloadObject);
        }

        private static byte[] SerializePayload(object payloadObject)
        {
            string jsonPayload = JsonSerializer.Serialize(payloadObject);
            return Encoding.UTF8.GetBytes(jsonPayload);
        }
    }

    internal struct CommandOnlyProfilerMessage : IProfilerMessage
    {
        public IpcCommand Command { get; }
        public byte[] Payload { get; } = Array.Empty<byte>();

        public CommandOnlyProfilerMessage(IpcCommand command)
        {
            Command = command;
        }
    }
}
