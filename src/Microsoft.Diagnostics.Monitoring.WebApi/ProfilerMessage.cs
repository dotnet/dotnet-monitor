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
        SimpleCommand,
        JsonCommand
    };

    internal enum ProfilerCommand : short
    {
        Unknown,
        Ok,
        Error,
        Callstack,
        CaptureParameter,
    };

    internal interface IProfilerMessage
    {
        public ProfilerMessageType MessageType { get; set; }
        public ProfilerCommand Command { get; set; }

        public byte[] SerializePayload();
    }

    internal ref struct RawProfilerMessage
    {
        public RawProfilerMessage()
        {
        }

        public ProfilerMessageType MessageType { get; set; } = ProfilerMessageType.Unknown;
        public ProfilerCommand Command { get; set; } = ProfilerCommand.Unknown;

        public byte[] Payload { get; set; } = Array.Empty<byte>();

    }

    internal struct JsonProfilerMessage : IProfilerMessage
    {
        public ProfilerMessageType MessageType { get; set; } = ProfilerMessageType.JsonCommand;
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

        public ProfilerMessageType MessageType { get; set; } = ProfilerMessageType.SimpleCommand;
        public ProfilerCommand Command { get; set; } = ProfilerCommand.Unknown;

        // This is currently unsupported, but some possible future additions:
        // Parameter Metadata. (I.e. IMetadataImport.GetMethodProps + signature resolution)
        // Resolve frame offsets (Resolving absolute native address to relative offset then convert to IL using IL-to-native maps.
        public int Parameter { get; set; }

        public byte[] SerializePayload()
        {
            return BitConverter.GetBytes(Parameter);
        }
    }
}
