// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring
{
    internal class ParameterCapturingPayload
    {
        public TimeSpan Duration { get; set; }

        public string[] FqMethodNames { get; set; } = Array.Empty<string>();
    }

    internal enum ProfilerMessageType : short
    {
        Unknown,
        SimpleMessage,
        JsonMessage
    };

    internal enum ProfilerCommand : short
    {
        Unknown,
        Status,
        Callstack,
        CaptureParameters,
    };

    internal interface IProfilerMessage
    {
        public ProfilerMessageType MessageType { get; set; }
        public ProfilerCommand Command { get; set; }

        public byte[] SerializePayload();
    }

    internal struct JsonProfilerMessage : IProfilerMessage
    {
        public ProfilerMessageType MessageType { get; set; } = ProfilerMessageType.JsonMessage;
        public ProfilerCommand Command { get; set; } = ProfilerCommand.Unknown;

        public object Payload { get; set; }

        public JsonProfilerMessage(object t)
        {
            Payload = t;
        }

        public byte[] SerializePayload()
        {
            string jsonPayload = JsonSerializer.Serialize(Payload);
            return Encoding.UTF8.GetBytes(jsonPayload);
        }
    }

    internal struct SimpleProfilerMessage : IProfilerMessage
    {
        public SimpleProfilerMessage()
        {
        }

        public ProfilerMessageType MessageType { get; set; } = ProfilerMessageType.SimpleMessage;
        public ProfilerCommand Command { get; set; } = ProfilerCommand.Unknown;

        public int Parameter { get; set; }

        public byte[] SerializePayload()
        {
            return BitConverter.GetBytes(Parameter);
        }
    }
}
