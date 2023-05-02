// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.S3Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.S3StorageTests.UnitTests
{
    public class MultiPartUploadStreamTests
    {
        public enum EWriteOperation
        {
            WithMemory,
            WithBuffer
        }


        private readonly InMemoryStorage _s3 = new InMemoryStorage("bucket", "key");

        private static IEnumerable<byte[]> WithBytesReturned(int totalBytes)
        {
            while (totalBytes > 0)
            {
                var length = Random.Shared.Next(1, Math.Min(totalBytes, 1024));
                var bytes = new byte[length];
                Random.Shared.NextBytes(bytes);
                yield return bytes;
                totalBytes -= length;
            }
        }

        [Fact]
        public async Task ItShouldWorkWithBufferSizeGreaterThanMinimum()
        {
            const int BufferSize = MultiPartUploadStream.MinimumSize + MultiPartUploadStream.MinimumSize / 2;
            var uploadId = (await _s3.InitMultiPartUploadAsync(null, CancellationToken.None));
            await using var stream = new MultiPartUploadStream(_s3, "bucket", "key", uploadId, BufferSize);

            const int Part1ExpectedSize = BufferSize;
            const int Part2ExpectedSize = MultiPartUploadStream.MinimumSize / 2;
            var allBytes = new List<byte>();
            foreach (var bytes in WithBytesReturned(Part1ExpectedSize + Part2ExpectedSize))
            {
                await stream.WriteAsync(bytes, CancellationToken.None);
                allBytes.AddRange(bytes);
            }

            await stream.FinalizeAsync(CancellationToken.None);

            Assert.Equal(2, stream.Parts.Count);
            Assert.True(_s3.Uploads.TryGetValue(uploadId, out List<InMemoryStorage.StorageData> data));
            Assert.Equal(2, data.Count);

            Assert.Equal(Part1ExpectedSize, data[0].Size);
            Assert.Equal(allBytes.Take(Part1ExpectedSize).ToArray(), data[0].Bytes());

            Assert.Equal(allBytes.Skip(Part1ExpectedSize).ToArray(), data[1].Bytes());
            Assert.Equal(Part2ExpectedSize, data[1].Size);
        }

        [Fact]
        public async Task ItShouldWriteMultipleParts()
        {
            var uploadId = (await _s3.InitMultiPartUploadAsync(null, CancellationToken.None));
            await using var stream = new MultiPartUploadStream(_s3, "bucket", "key", uploadId, 1024);

            const int Part1ExpectedSize = MultiPartUploadStream.MinimumSize;
            const int Part2ExpectedSize = MultiPartUploadStream.MinimumSize;
            const int Part3ExpectedSize = MultiPartUploadStream.MinimumSize - 1024;

            var allBytes = new List<byte>();
            foreach (var bytes in WithBytesReturned(Part1ExpectedSize + Part2ExpectedSize + Part3ExpectedSize))
            {
                await stream.WriteAsync(bytes, CancellationToken.None);
                allBytes.AddRange(bytes);
            }

            await stream.FinalizeAsync(CancellationToken.None);

            Assert.Equal(3, stream.Parts.Count);
            Assert.True(_s3.Uploads.TryGetValue(uploadId, out List<InMemoryStorage.StorageData> data));
            Assert.Equal(3, data.Count);

            Assert.Equal(Part1ExpectedSize, data[0].Size);
            Assert.Equal(allBytes.Take(Part1ExpectedSize).ToArray(), data[0].Bytes());

            Assert.Equal(Part2ExpectedSize, data[1].Size);
            Assert.Equal(allBytes.Skip(Part1ExpectedSize).Take(Part2ExpectedSize).ToArray(), data[1].Bytes());

            Assert.Equal(allBytes.Skip(Part1ExpectedSize + Part2ExpectedSize).ToArray(), data[2].Bytes());
            Assert.Equal(Part3ExpectedSize, data[2].Size);
        }

        [Fact]
        public async Task ItShouldWriteSinglePartialPart()
        {
            var uploadId = (await _s3.InitMultiPartUploadAsync(null, CancellationToken.None));
            await using var stream = new MultiPartUploadStream(_s3, "bucket", "key", uploadId, 1024);

            const int PartExpectedSize = MultiPartUploadStream.MinimumSize - 1024;

            var allBytes = new List<byte>();
            foreach (var bytes in WithBytesReturned(PartExpectedSize))
            {
                await stream.WriteAsync(bytes, CancellationToken.None);
                allBytes.AddRange(bytes);
            }

            await stream.FinalizeAsync(CancellationToken.None);

            Assert.Single(stream.Parts);
            Assert.True(_s3.Uploads.TryGetValue(uploadId, out List<InMemoryStorage.StorageData> data));
            InMemoryStorage.StorageData storageItem = Assert.Single(data);
            Assert.Equal(PartExpectedSize, storageItem.Size);
            Assert.Equal(allBytes.ToArray(), storageItem.Bytes());
        }

        [Fact]
        public async Task ItShouldWriteSingleFullPart()
        {
            var uploadId = (await _s3.InitMultiPartUploadAsync(null, CancellationToken.None));
            await using var stream = new MultiPartUploadStream(_s3, "bucket", "key", uploadId, 1024);

            const int PartExpectedSize = MultiPartUploadStream.MinimumSize;
            var allBytes = new List<byte>();
            foreach (var bytes in WithBytesReturned(PartExpectedSize))
            {
                await stream.WriteAsync(bytes, CancellationToken.None);
                allBytes.AddRange(bytes);
            }

            await stream.FinalizeAsync(CancellationToken.None);

            Assert.Single(stream.Parts);
            Assert.True(_s3.Uploads.TryGetValue(uploadId, out List<InMemoryStorage.StorageData> data));
            InMemoryStorage.StorageData storageItem = Assert.Single(data); // the first entry contains meta information
            Assert.Equal(PartExpectedSize, storageItem.Size);
            Assert.Equal(allBytes.ToArray(), storageItem.Bytes());
        }

        [Fact]
        public async Task ItShouldWriteNothing()
        {
            var uploadId = (await _s3.InitMultiPartUploadAsync(null, CancellationToken.None));
            await using var stream = new MultiPartUploadStream(_s3, "bucket", "key", uploadId, 1024);
            await stream.FinalizeAsync(CancellationToken.None);

            Assert.Empty(stream.Parts);
            Assert.True(_s3.Uploads.TryGetValue(uploadId, out List<InMemoryStorage.StorageData> data));
            Assert.Empty(data);
        }
    }
}
