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

            int read;
            int total = 0;
            while (total < buffer.Length && 0 != (read = await stream.ReadAsync(buffer, total, buffer.Length - total, cancellationToken)))
            {
                total += read;
            }

            Assert.Equal(buffer.Length, total);

            return buffer;
        }
    }
}
