// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        public async Task SendMessage(IEndpointInfo endpointInfo, IProfilerMessage message, CancellationToken token)
        {
            if (message.PayloadType != ProfilerPayloadType.None &&
                message.Parameter != message.Payload.Length)
            {
                throw new ArgumentException(nameof(message));
            }

            string channelPath = ComputeChannelPath(endpointInfo);
            var endpoint = new UnixDomainSocketEndPoint(channelPath);
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            //Note that this is still getting built for 3.1 due to test app dependencies.

            await socket.ConnectAsync(endpoint);

            byte[] headersBuffer = new byte[sizeof(short) + sizeof(short) + sizeof(int)];
            var memoryStream = new MemoryStream(headersBuffer);
            using BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write((short)message.MessageType);
            writer.Write((short)message.PayloadType);
            writer.Write(message.Parameter);
            writer.Dispose();
            await socket.SendAsync(new ReadOnlyMemory<byte>(headersBuffer), SocketFlags.None, token);

            if (message.PayloadType != ProfilerPayloadType.None)
            {
                await socket.SendAsync(new ReadOnlyMemory<byte>(message.Payload), SocketFlags.None, token);
            }

            BasicProfilerMessage response = await ReceiveBasicMessageAsync(socket, token);
            if (response.MessageType != ProfilerMessageType.Status)
            {
                throw new InvalidOperationException("Received unexpected status message from server.");
            }
            Marshal.ThrowExceptionForHR(response.Parameter);
        }

        private static async Task<BasicProfilerMessage> ReceiveBasicMessageAsync(Socket socket, CancellationToken token)
        {
            byte[] recvBuffer = new byte[sizeof(short) + sizeof(short) + sizeof(int)];
            int received = await socket.ReceiveAsync(new Memory<byte>(recvBuffer), SocketFlags.None, token);
            if (received < recvBuffer.Length)
            {
                //TODO Figure out if fragmentation is possible over UDS.
                throw new InvalidOperationException($"Could not receive message from server.");
            }

            int readIndex = 0;
            ProfilerMessageType messageType = (ProfilerMessageType)BitConverter.ToInt16(recvBuffer, startIndex: readIndex);
            readIndex += sizeof(short);

            ProfilerPayloadType payloadType = (ProfilerPayloadType)BitConverter.ToInt16(recvBuffer, startIndex: readIndex);
            if (payloadType != ProfilerPayloadType.None)
            {
                throw new InvalidOperationException("Received unexpected payload type from server.");
            }
            readIndex += sizeof(short);

            int parameter = BitConverter.ToInt32(recvBuffer, startIndex: readIndex);
            readIndex += sizeof(int);
            Debug.Assert(readIndex == recvBuffer.Length);

            return new BasicProfilerMessage(messageType, parameter);
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
