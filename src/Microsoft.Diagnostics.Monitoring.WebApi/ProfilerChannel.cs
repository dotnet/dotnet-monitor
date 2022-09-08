// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

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

        public int Parameter { get; set; }
    }

    /// <summary>
    /// Communicates with the profiler, using a Unix Domain Socket.
    /// </summary>
    internal static class ProfilerChannel
    {
        public static async Task<ProfilerMessage> SendMessage(IEndpointInfo endpointInfo, ProfilerMessage message, CancellationToken token)
        {
#if NET6_0_OR_GREATER
            string channelPath = Environment.ExpandEnvironmentVariables(FormattableString.Invariant(@$"%TEMP%\{endpointInfo.RuntimeInstanceCookie:D}.sock"));
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
#else
            return await Task.FromException<ProfilerMessage>(new NotImplementedException());
#endif
        }
    }
}
