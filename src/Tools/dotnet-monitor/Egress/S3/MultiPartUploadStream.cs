// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3.Model;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3;

internal class MultiPartUploadStream : Stream
{
    private readonly byte[] _buffer;
    private int _offset;
    private readonly string _bucketName;
    private readonly string _objectKey;
    private readonly string _uploadId;
    private readonly IS3Storage _client;
    private readonly List<PartETag> _parts = new();
    public List<PartETag> Parts => _parts.ToList();
    public bool Closed { get; private set; }
    private int _position;
    public const int MinimumSize = 5 * 1024 * 1024; // the minimum size of an upload part (except for the last part)
    private readonly int _bufferSize;

    private readonly byte[] _syncBuffer;
    private List<byte> _syncTempBuffer;
    private SemaphoreSlim _semaphore;
    private int _syncOffset;
    int counter;

    public MultiPartUploadStream(IS3Storage client, string bucketName, string objectKey, string uploadId, int bufferSize)
    {
        _bufferSize = Math.Max(bufferSize, MinimumSize); // has to be at least the minimum
        _buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        _syncBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        _syncTempBuffer = new();
        _offset = 0;
        _syncOffset = 0;
        _client = client;
        _bucketName = bucketName;
        _objectKey = objectKey;
        _uploadId = uploadId;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        await DoWriteAsync(false, cancellationToken);
    }

    public async Task FinalizeAsync(CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0)
            return;
        await DoWriteAsync(true, cancellationToken);
    }

    public async Task FinalizeSyncAsync(CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0)
            return;


        while (_syncOffset > 0 || _offset > 0)
        {
            Console.WriteLine("Finalize Loop: " + _syncOffset.ToString() + " | " + cancellationToken.IsCancellationRequested);

            if (Closed)
                throw new ObjectDisposedException(nameof(MultiPartUploadStream));

            int BytesAvailableInBuffer() { return _bufferSize - _offset; }
            do
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    Console.WriteLine("Do Loop: " + _syncOffset);
                    int bytesToCopy = Math.Min(_syncOffset, BytesAvailableInBuffer());
                    Console.WriteLine("IsCancellationRequested: " + cancellationToken.IsCancellationRequested);
                    Console.WriteLine("Sync Offset: " + _syncOffset);
                    Console.WriteLine("Offset: " + _offset);

                    var str = System.Text.Encoding.Default.GetString(_syncTempBuffer.ToArray());

                    //Console.WriteLine("SyncTempBuffer: " + str);

                    _syncTempBuffer.GetRange(_position, bytesToCopy).ToArray().CopyTo(_syncBuffer.AsMemory(_offset));
                    _offset += bytesToCopy; // move the offset of the stream buffer
                    _syncOffset -= bytesToCopy;
                    _position += bytesToCopy; // move global position

                    await DoSyncWriteAsync(false, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            } while (_syncOffset > 0);

            await Task.Delay(500, cancellationToken); // arbitrary
        }
    }

    public async Task StartAsyncLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || _syncOffset > 0 || _offset > 0)
        {
            Console.WriteLine("Loop: " + _syncOffset.ToString() + " | " + cancellationToken.IsCancellationRequested);

            if (Closed)
                throw new ObjectDisposedException(nameof(MultiPartUploadStream));

            int BytesAvailableInBuffer() { return _bufferSize - _offset; }
            do
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    Console.WriteLine("Do Loop: " + _syncOffset);
                    int bytesToCopy = Math.Min(_syncOffset, BytesAvailableInBuffer());
                    Console.WriteLine("IsCancellationRequested: " + cancellationToken.IsCancellationRequested);
                    Console.WriteLine("Sync Offset: " + _syncOffset);
                    Console.WriteLine("Offset: " + _offset);

                    var str = System.Text.Encoding.Default.GetString(_syncTempBuffer.ToArray());

                    //Console.WriteLine("SyncTempBuffer: " + str);

                    _syncTempBuffer.GetRange(_position, bytesToCopy).ToArray().CopyTo(_syncBuffer.AsMemory(_offset));
                    _offset += bytesToCopy; // move the offset of the stream buffer
                    _syncOffset -= bytesToCopy;
                    _position += bytesToCopy; // move global position

                    // part buffer is full -> trigger upload of part
                    if (BytesAvailableInBuffer() == 0 || cancellationToken.IsCancellationRequested)
                        await DoSyncWriteAsync(false, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            } while (_syncOffset > 0);

            await Task.Delay(500, cancellationToken); // arbitrary
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        int BytesAvailableInBuffer() { return _bufferSize - _offset; }
        int count = buffer.Length;
        int offset = 0;
        do
        {
            int bytesToCopy = Math.Min(count, BytesAvailableInBuffer());
            buffer.Slice(offset, bytesToCopy).CopyTo(_buffer.AsMemory(_offset));
            _offset += bytesToCopy; // move the offset of the stream buffer
            offset += bytesToCopy; // move offset of part buffer
            count -= bytesToCopy; // reduce amount of bytes which still needs to be written
            _position += bytesToCopy; // move global position

            // part buffer is full -> trigger upload of part
            if (BytesAvailableInBuffer() == 0)
                await DoWriteAsync(false, cancellationToken);
        } while (count > 0);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken);
    }

    private async Task DoWriteAsync(bool allowPartialWrite, CancellationToken cancellationToken)
    {
        if (_offset == 0) // no data
            return;

        if (_offset < MinimumSize && !allowPartialWrite) // buffer not full
            return;

        await using var stream = new MemoryStream(_buffer, 0, _offset);
        stream.Position = 0;
        // use _parts.Count + 1 to avoid a part #0 (part numbers for AWS must not be less than 1)
        var eTag = await _client.UploadPartAsync(_uploadId, _parts.Count + 1, _offset, stream, cancellationToken);
        _parts.Add(eTag);
        _offset = 0;
    }

    private async Task DoSyncWriteAsync(bool allowPartialWrite, CancellationToken cancellationToken)
    {
        Console.WriteLine("DoSyncWriteAsync.");

        if (_offset == 0) // no data
            return;

        /*
        if (_offset < MinimumSize && !allowPartialWrite) // buffer not full
            return;
        */

        //var str = System.Text.Encoding.Default.GetString(_syncBuffer);

        //Console.WriteLine("DoSyncWriteAsync: " + str);

        await using var stream = new MemoryStream(_syncBuffer, 0, _offset);
        stream.Position = 0;
        // use _parts.Count + 1 to avoid a part #0 (part numbers for AWS must not be less than 1)
        var eTag = await _client.UploadPartAsync(_uploadId, _parts.Count + 1, _offset, stream, cancellationToken);
        _parts.Add(eTag);
        _offset = 0;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var str = System.Text.Encoding.Default.GetString(buffer);

        Console.WriteLine("B: " + counter);

        if (Closed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        _semaphore.Wait();
        try
        {
            Console.WriteLine("M: " + counter);

            _syncTempBuffer.AddRange(buffer.AsMemory().Slice(offset, count).ToArray().AsEnumerable());

            _syncOffset += count;
        }
        finally
        {
            _semaphore.Release();
        }

        Console.WriteLine("E: " + counter);

        counter += 1;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !Closed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Close()
    {
        if (Closed)
            return;
        Closed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        base.Close();
    }
}
