using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3;

internal class MultiPartUploadStream : Stream
{
    private readonly byte[] _buffer;
    private int _offset;
    private readonly string _bucketName;
    private readonly string _objectKey;
    private readonly string _uploadId;
    private readonly IAmazonS3 _client;
    private readonly List<PartETag> _parts = new();
    public List<PartETag> Parts => _parts.ToList();
    public bool Disposed { get; private set; }
    private int _position;
    public const int MinimumSize = 5 * 1024 * 1024; // the minimum size of an upload part (except for the last part)

    public MultiPartUploadStream(IAmazonS3 client, string bucketName, string objectKey, string uploadId, int bufferSize)
    {
        bufferSize = Math.Max(bufferSize, MinimumSize); // has to be at least the minimum
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        _offset = 0;
        _client = client;
        _bucketName = bucketName;
        _objectKey = objectKey;
        _uploadId = uploadId;
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0 || _offset < MinimumSize)
            return;
        await DoWriteAsync(cancellationToken);
    }

    public async Task FinalizeAsync(CancellationToken cancellationToken)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));
        if (_offset == 0)
            return;
        await DoWriteAsync(cancellationToken);
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

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(MultiPartUploadStream));

        int BytesAvailableInBuffer() { return _buffer.Length - _offset;}
        do
        {
            int bytesToCopy = Math.Min(count, BytesAvailableInBuffer()); 
            Array.Copy(buffer, offset, _buffer, _offset, bytesToCopy);
            offset += bytesToCopy; // move offset of part buffer
            count -= bytesToCopy; // reduce amount of bytes which still needs to be written
            _position += bytesToCopy; // move global position

            // part buffer is full -> trigger upload of part
            if (BytesAvailableInBuffer() == 0)
                await DoWriteAsync(cancellationToken); 
        } while (count > 0);
    }

    private async Task DoWriteAsync(CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(_buffer, 0, _offset);
        stream.Position = 0;
        var uploadRequest = new UploadPartRequest
        {
            BucketName = _bucketName,
            Key = _objectKey, 
            InputStream = stream, 
            PartSize = _offset,
            UploadId = _uploadId,
            PartNumber = _parts.Count
        };
        var response = await _client.UploadPartAsync(uploadRequest, cancellationToken);
        _parts.Add(new PartETag(response.PartNumber, response.ETag));
        _offset = 0;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !Disposed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed)
            return;
        Disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        if (Disposed)
            return;
        Disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        await base.DisposeAsync();
    }
}
