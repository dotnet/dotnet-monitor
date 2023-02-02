// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class StreamExtensions
    {
        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[length];
            Memory<byte> memory = buffer;

            int read;
            int total = 0;
            while (total < buffer.Length && 0 != (read = await stream.ReadAsync(memory.Slice(total), cancellationToken)))
            {
                total += read;
            }

            Assert.Equal(buffer.Length, total);

            return buffer;
        }
    }
}
