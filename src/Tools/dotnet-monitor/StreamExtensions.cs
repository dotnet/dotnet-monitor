// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class StreamExtensions
    {
        public static async Task<bool> HasSameContentAsync(
            this Stream thisStream,
            Stream otherStream,
            CancellationToken cancellationToken)
        {
            byte[] thisBuffer = ArrayPool<byte>.Shared.Rent(StreamDefaults.BufferSize);
            Array.Fill<byte>(thisBuffer, 0);

            byte[] otherBuffer = ArrayPool<byte>.Shared.Rent(StreamDefaults.BufferSize);
            Array.Fill<byte>(otherBuffer, 0);

            try
            {
                Memory<byte> thisMemory = thisBuffer;
                Memory<byte> otherMemory = otherBuffer;

                while (true)
                {
                    // Fill buffer; read will only be less than buffer size
                    // if encounter end of stream.
                    int thisRead = await thisStream.ReadAtLeastAsync(
                        thisMemory,
                        thisMemory.Length,
                        throwOnEndOfStream: false,
                        cancellationToken);

                    // Fill buffer; read will only be less than buffer size
                    // if encounter end of stream.
                    int otherRead = await otherStream.ReadAtLeastAsync(
                        otherMemory,
                        otherMemory.Length,
                        throwOnEndOfStream: false,
                        cancellationToken);

                    if (thisRead != otherRead || !thisMemory.Span.SequenceEqual(otherMemory.Span))
                    {
                        return false;
                    }

                    // If read is less than buffer size, then end of streams
                    // were encountered. Since the last segment of the stream
                    // content were the same, the entire stream contents were
                    // the same.
                    if (thisRead < thisMemory.Length)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(thisBuffer);
                ArrayPool<byte>.Shared.Return(otherBuffer);
            }
        }

#if !NET7_0_OR_GREATER
        public static async ValueTask<int> ReadAtLeastAsync(
            this Stream stream,
            Memory<byte> buffer,
            int minimumBytes,
            bool throwOnEndOfStream = true,
            CancellationToken cancellationToken = default)
        {
            int totalRead = 0;
            while (totalRead < minimumBytes)
            {
                int read = await stream.ReadAsync(buffer.Slice(totalRead), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    if (throwOnEndOfStream)
                    {
                        throw new EndOfStreamException();
                    }

                    return totalRead;
                }

                totalRead += read;
            }

            return totalRead;
        }
#endif
    }
}
