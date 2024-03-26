// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring
{
    internal enum CommandSet : ushort
    {
        DotnetMonitor,
        Profiler,
        ManagedInProc
    }

    internal enum DotnetMonitorCommand : ushort
    {
        Status
    };

    internal enum ProfilerCommand : ushort
    {
        Callstack
    };

    internal enum ManagedInProcCommand : ushort
    {
        StartCapturingParameters,
        StopCapturingParameters
    };

    internal interface IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; }
    }

    internal struct JsonProfilerMessage : IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; }

        public JsonProfilerMessage(ManagedInProcCommand command, object payloadObject)
            : this((ushort)Monitoring.CommandSet.ManagedInProc, (ushort)command, payloadObject) { }

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

    internal struct CommandOnlyProfilerMessage : IProfilerMessage
    {
        public ushort CommandSet { get; }
        public ushort Command { get; }
        public byte[] Payload { get; } = Array.Empty<byte>();

        public CommandOnlyProfilerMessage(ProfilerCommand command)
            : this((ushort)Monitoring.CommandSet.Profiler, (ushort)command) { }

        public CommandOnlyProfilerMessage(ManagedInProcCommand command)
            : this((ushort)Monitoring.CommandSet.ManagedInProc, (ushort)command) { }

        public CommandOnlyProfilerMessage(ushort commandSet, ushort command)
        {
            CommandSet = commandSet;
            Command = command;
        }
    }
}
