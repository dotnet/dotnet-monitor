// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;


#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.Monitoring
#else
namespace Microsoft.Diagnostics.Monitoring
#endif
{
    public enum CommandSet : ushort
    {
        ServerResponse,
        Profiler,
        StartupHook
    }

    public enum ServerResponseCommand : ushort
    {
        Status
    };

    public enum ProfilerCommand : ushort
    {
        Callstack
    };

    public enum StartupHookCommand : ushort
    {
        StartCapturingParameters,
        StopCapturingParameters
    };

    public interface IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; }
    }

    public struct JsonProfilerMessage : IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; }

        public JsonProfilerMessage(StartupHookCommand command, object payloadObject)
            : this((ushort)Monitoring.CommandSet.StartupHook, (ushort)command, payloadObject) { }

        public JsonProfilerMessage(ushort commandSet, ushort command, object payloadObject)
        {
            CommandSet = commandSet;
            Command = command;
            Payload = SerializePayload(payloadObject);
        }

        private static byte[] SerializePayload(object payloadObject)
        {
            string jsonPayload = JsonSerializer.Serialize(payloadObject);
            return Encoding.UTF8.GetBytes(jsonPayload);
        }
    }

    public struct CommandOnlyProfilerMessage : IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; } = Array.Empty<byte>();

        public CommandOnlyProfilerMessage(ProfilerCommand command)
            : this((ushort)Monitoring.CommandSet.Profiler, (ushort)command) { }

        public CommandOnlyProfilerMessage(StartupHookCommand command)
            : this((ushort)Monitoring.CommandSet.StartupHook, (ushort)command) { }

        public CommandOnlyProfilerMessage(ushort commandSet, ushort command)
        {
            CommandSet = commandSet;
            Command = command;
        }
    }
}
