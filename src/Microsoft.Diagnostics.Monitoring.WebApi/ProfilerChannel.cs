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

        public async Task<SimpleProfilerMessage> SendMessage(IEndpointInfo endpointInfo, IProfilerMessage message, CancellationToken token)
        {
            string channelPath = ComputeChannelPath(endpointInfo);
            var endpoint = new UnixDomainSocketEndPoint(channelPath);
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            //Note that this is still getting built for 3.1 due to test app dependencies.

            await socket.ConnectAsync(endpoint);

            byte[] messagePayload = message.SerializePayload();

            byte[] buffer = new byte[sizeof(short) + sizeof(short) + sizeof(int) + messagePayload.Length];
            var memoryStream = new MemoryStream(buffer);
            using BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write((short)message.MessageType);
            writer.Write((short)message.Command);
            writer.Write(messagePayload.Length);
            writer.Write(messagePayload);
            writer.Dispose();
            await socket.SendAsync(new ReadOnlyMemory<byte>(buffer), SocketFlags.None, token);

            byte[] recvBuffer = new byte[sizeof(short) + sizeof(short) + sizeof(int)];
            int received = await socket.ReceiveAsync(new Memory<byte>(recvBuffer), SocketFlags.None, token);
            if (received < recvBuffer.Length)
            {
                //TODO Figure out if fragmentation is possible over UDS.
                throw new InvalidOperationException($"Could not receive message from server. {received} - {recvBuffer.Length}");
            }

            ProfilerMessageType messageType = (ProfilerMessageType)BitConverter.ToInt16(recvBuffer, startIndex: 0);
            ProfilerCommand command = (ProfilerCommand)BitConverter.ToInt16(recvBuffer, startIndex: 2);

            if (messageType != ProfilerMessageType.SimpleCommand)
            {
                throw new InvalidOperationException($"Received unexpected status message from server. {messageType}");
            }

            if (command != ProfilerCommand.Ok && command != ProfilerCommand.Error)
            {
                throw new InvalidOperationException($"Received unexpected command from server. {command}");
            }

            return new SimpleProfilerMessage
            {
                MessageType = messageType,
                Parameter = BitConverter.ToInt32(recvBuffer, startIndex: 4)
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
