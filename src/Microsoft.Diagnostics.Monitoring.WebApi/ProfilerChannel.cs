// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.Design;
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
            string channelPath = ComputeChannelPath(endpointInfo);
            var endpoint = new UnixDomainSocketEndPoint(channelPath);
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            //Note that this is still getting built for 3.1 due to test app dependencies.

            await socket.ConnectAsync(endpoint);

            byte[] payloadBuffer = message.SerializePayload();

            byte[] headersBuffer = new byte[sizeof(short) + sizeof(short) + sizeof(int)];
            var memoryStream = new MemoryStream(headersBuffer);
            using BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write((short)message.MessageType);
            writer.Write((short)message.Command);
            writer.Write(payloadBuffer.Length);
            writer.Dispose();
            await socket.SendAsync(new ReadOnlyMemory<byte>(headersBuffer), SocketFlags.None, token);
            await socket.SendAsync(new ReadOnlyMemory<byte>(payloadBuffer), SocketFlags.None, token);

            int received = await socket.ReceiveAsync(new Memory<byte>(headersBuffer), SocketFlags.None, token);
            if (received < headersBuffer.Length)
            {
                //TODO Figure out if fragmentation is possible over UDS.
                throw new InvalidOperationException($"Could not receive message from server. {received} - {headersBuffer.Length}");
            }

            ProfilerMessageType messageType = (ProfilerMessageType)BitConverter.ToInt16(headersBuffer, startIndex: 0);
            if (messageType != ProfilerMessageType.SimpleMessage)
            {
                throw new InvalidOperationException($"Received unexpected status message from server. {messageType}");
            }

            ProfilerCommand command = (ProfilerCommand)BitConverter.ToInt16(headersBuffer, startIndex: 2);
            if (command != ProfilerCommand.Status)
            {
                throw new InvalidOperationException($"Received unexpected status message from server. {command}");
            }

            int hresult = BitConverter.ToInt32(headersBuffer, startIndex: 4);
            Marshal.ThrowExceptionForHR(hresult);
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
