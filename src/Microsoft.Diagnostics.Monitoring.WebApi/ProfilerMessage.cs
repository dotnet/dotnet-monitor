// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring
{
    internal enum IpcCommand : short
    {
        Unknown,
        Status,
        Callstack
    };

    internal interface IProfilerMessage
    {
        public IpcCommand Command { get; set; }
        public byte[] Payload { get; set; }
    }

    internal struct JsonProfilerMessage : IProfilerMessage
    {
        public IpcCommand Command { get; set; } = IpcCommand.Unknown;
        public byte[] Payload { get; set; }

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
        public IpcCommand Command { get; set; } = IpcCommand.Unknown;
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public CommandOnlyProfilerMessage(IpcCommand command)
        {
            Command = command;
        }
    }
}
