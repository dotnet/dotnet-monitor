// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal enum ProfilerMessageType : short
    {
        OK,
        Error,
        Callstack
    };

    internal struct ProfilerMessage
    {
        public ProfilerMessageType MessageType { get; set; }

        // This is currently unsupported, but some possible future additions:
        // Parameter Metadata. (I.e. IMetadataImport.GetMethodProps + signature resolution)
        // Resolve frame offsets (Resolving absolute native address to relative offset then convert to IL using IL-to-native maps.
        public int Parameter { get; set; }
    }

    /// <summary>
    /// Communicates with the profiler, using a Unix Domain Socket.
    /// </summary>
    internal sealed class ProfilerChannel
    {
        private IOptionsMonitor<StorageOptions> _storageOptions;

        public ProfilerChannel(IOptionsMonitor<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public async Task<ProfilerMessage> SendMessage(IEndpointInfo endpointInfo, ProfilerMessage message, CancellationToken token)
        {
            string channelPath = ComputeChannelPath(endpointInfo);
            var endpoint = new UnixDomainSocketEndPoint(channelPath);
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            //Note that this is still getting built for 3.1 due to test app dependencies.

            await socket.ConnectAsync(endpoint);

            byte[] buffer = new byte[sizeof(short) + sizeof(int)];
            var memoryStream = new MemoryStream(buffer);
            using BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write((short)message.MessageType);
            writer.Write(message.Parameter);
            writer.Dispose();

            await socket.SendAsync(new ReadOnlyMemory<byte>(buffer), SocketFlags.None, token);
            int received = await socket.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None, token);
            if (received < buffer.Length)
            {
                //TODO Figure out if fragmentation is possible over UDS.
                throw new InvalidOperationException("Could not receive message from server.");
            }

            return new ProfilerMessage
            {
                MessageType = (ProfilerMessageType)BitConverter.ToInt16(buffer, startIndex: 0),
                Parameter = BitConverter.ToInt32(buffer, startIndex: 2)
            };
        }

        private string ComputeChannelPath(IEndpointInfo endpointInfo)
        {
            string defaultSharedPath = _storageOptions.CurrentValue.DefaultSharedPath;
            if (string.IsNullOrEmpty(_storageOptions.CurrentValue.DefaultSharedPath))
            {
                //Note this fallback does not work well for sidecar scenarios.
                defaultSharedPath = Path.GetTempPath();
            }
            return Path.Combine(defaultSharedPath, FormattableString.Invariant($"{endpointInfo.RuntimeInstanceCookie:D}.sock"));
        }
    }
}
