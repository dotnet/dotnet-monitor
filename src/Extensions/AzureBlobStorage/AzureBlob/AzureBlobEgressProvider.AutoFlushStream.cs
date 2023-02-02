// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal sealed partial class AzureBlobEgressProvider
    {
        /// <summary>
        /// Automatically flushes the stream after a certain amount of bytes have been written.
        /// </summary>
        private sealed class AutoFlushStream : Stream
        {
            private readonly Stream _baseStream;
            private readonly long _flushSize;
            private long _written;

            public AutoFlushStream(Stream stream, long flushSize)
            {
                _flushSize = flushSize >= 0 ? flushSize : throw new ArgumentOutOfRangeException(nameof(flushSize));
                _baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            }

            public override bool CanRead => _baseStream.CanRead;
            public override bool CanSeek => _baseStream.CanSeek;
            public override bool CanWrite => _baseStream.CanWrite;
            public override long Length => _baseStream.Length;
            public override bool CanTimeout => _baseStream.CanTimeout;
            public override int WriteTimeout { get => _baseStream.WriteTimeout; set => _baseStream.WriteTimeout = value; }
            public override int ReadTimeout { get => _baseStream.ReadTimeout; set => _baseStream.ReadTimeout = value; }
            public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }
            public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
            public override void SetLength(long value) => _baseStream.SetLength(value);
            public override void Close() => _baseStream.Close();
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                _baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                _baseStream.ReadAsync(buffer, cancellationToken);
            public override int ReadByte() => _baseStream.ReadByte();

            //CONSIDER These are not really used, but should still autoflush.
            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
                _baseStream.BeginRead(buffer, offset, count, callback, state);
            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
                _baseStream.BeginWrite(buffer, offset, count, callback, state);
            public override int EndRead(IAsyncResult asyncResult) => _baseStream.EndRead(asyncResult);
            public override void EndWrite(IAsyncResult asyncResult) => _baseStream.EndWrite(asyncResult);

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
                ProcessWrite(count);
            }

            public override void WriteByte(byte value)
            {
                _baseStream.WriteByte(value);
                ProcessWrite(1);
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await _baseStream.WriteAsync(buffer, cancellationToken);
                await ProcessWriteAsync(buffer.Length, cancellationToken);
            }

            public override void Flush()
            {
                _baseStream.Flush();
                _written = 0;
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await _baseStream.FlushAsync(cancellationToken);
                _written = 0;
            }

            private void ProcessWrite(int count)
            {
                _written += count;
                if (_written >= _flushSize)
                {
                    Flush();
                }
            }

            private Task ProcessWriteAsync(int count, CancellationToken cancellationToken)
            {
                _written += count;
                if (_written >= _flushSize)
                {
                    return FlushAsync(cancellationToken);
                }
                return Task.CompletedTask;
            }
        }
    }
}
