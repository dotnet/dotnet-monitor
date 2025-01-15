// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Globalization;
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
    public sealed class ProfilerChannel
    {
        private const int MaxPayloadSize = 4 * 1024 * 1024; // 4 MiB

        private IOptionsMonitor<StorageOptions> _storageOptions;

        public ProfilerChannel(IOptionsMonitor<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public async Task SendMessage(IEndpointInfo endpointInfo, IProfilerMessage message, CancellationToken token)
        {
            if (message.Payload.Length > MaxPayloadSize)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_ProfilerPayloadTooLarge,
                        message.Payload.Length,
                        MaxPayloadSize),
                    nameof(message));
            }

            string channelPath = ComputeChannelPath(endpointInfo);
            var endpoint = new UnixDomainSocketEndPoint(channelPath);
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            //Note that this is still getting built for 3.1 due to test app dependencies.

            await socket.ConnectAsync(endpoint);

            byte[] headersBuffer = new byte[sizeof(ushort) + sizeof(ushort) + sizeof(int)];
            var memoryStream = new MemoryStream(headersBuffer);
            using BinaryWriter writer = new BinaryWriter(memoryStream);
            writer.Write(message.CommandSet);
            writer.Write(message.Command);
            writer.Write(message.Payload.Length);
            writer.Dispose();
            await socket.SendAsync(new ReadOnlyMemory<byte>(headersBuffer), SocketFlags.None, token);

            if (message.Payload.Length != 0)
            {
                await socket.SendAsync(new ReadOnlyMemory<byte>(message.Payload), SocketFlags.None, token);
            }

            int hresult = await ReceiveStatusMessageAsync(socket, token);
            Marshal.ThrowExceptionForHR(hresult);
        }

        private static async Task<int> ReceiveStatusMessageAsync(Socket socket, CancellationToken token)
        {
            byte[] headersBuffer = new byte[sizeof(ushort) + sizeof(ushort) + sizeof(int)];
            int received = await socket.ReceiveAsync(new Memory<byte>(headersBuffer), SocketFlags.None, token);
            if (received < headersBuffer.Length)
            {
                //TODO Figure out if fragmentation is possible over UDS.
                throw new InvalidOperationException("Could not receive message from server.");
            }

            int headerOffset = 0;
            ushort commandSet = BitConverter.ToUInt16(headersBuffer, startIndex: headerOffset);
            headerOffset += sizeof(ushort);

            if (commandSet != (ushort)CommandSet.ServerResponse)
            {
                throw new InvalidOperationException("Received unexpected command set from server.");
            }

            ushort command = BitConverter.ToUInt16(headersBuffer, startIndex: headerOffset);
            headerOffset += sizeof(ushort);

            if (command != (ushort)ServerResponseCommand.Status)
            {
                throw new InvalidOperationException("Received unexpected command from server.");
            }

            int payloadSize = BitConverter.ToInt32(headersBuffer, startIndex: headerOffset);
            headerOffset += sizeof(int);

            Debug.Assert(headerOffset == headersBuffer.Length);

            //
            // End of header, headerOffset should not be used after this point
            //

            byte[] payloadBuffer = new byte[sizeof(int)];
            if (payloadSize != payloadBuffer.Length)
            {
                throw new InvalidOperationException("Received unexpected payload size from server.");
            }

            received = await socket.ReceiveAsync(new Memory<byte>(payloadBuffer), SocketFlags.None, token);
            if (received < payloadBuffer.Length)
            {
                throw new InvalidOperationException("Could not receive message payload from server.");
            }

            return BitConverter.ToInt32(payloadBuffer);
        }

        private string ComputeChannelPath(IEndpointInfo endpointInfo)
        {
            string? defaultSharedPath = _storageOptions.CurrentValue.DefaultSharedPath;
            if (string.IsNullOrEmpty(defaultSharedPath))
            {
                //Note this fallback does not work well for sidecar scenarios.
                defaultSharedPath = Path.GetTempPath();
            }
            return Path.Combine(defaultSharedPath, FormattableString.Invariant($"{endpointInfo.RuntimeInstanceCookie:D}.sock"));
        }
    }
}
