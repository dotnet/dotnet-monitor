// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Diagnostics.Monitoring.Extension.S3Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.S3StorageTests.UnitTests
{
    public class InMemoryStorage : IS3Storage
    {
        public class StorageData
        {
            private Stream _content;
            public long Size { get; set; }
            public string Name { get; }
            public DateTime LastModified { get; set; } = DateTime.UtcNow;
            public string ETag { get; }

            public StorageData(string name)
            {
                Name = name;
                ETag = Guid.NewGuid().ToString("N");

                Size = Random.Shared.Next(10240);
                var buffer = new byte[Size];
                Random.Shared.NextBytes(buffer);
                _content = new MemoryStream(buffer);
            }

            public void SetContent(Stream stream, long? size = null)
            {
                Size = size.GetValueOrDefault(stream.Length);

                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                _content = memoryStream;
            }

            public byte[] Bytes() => Content().ToArray();

            public MemoryStream Content()
            {
                var memoryStream = new MemoryStream();
                _content.CopyTo(memoryStream);
                memoryStream.Position = 0;
                _content.Position = 0;
                return memoryStream;
            }
        }

        private readonly string _bucketName;
        private readonly string _objectKey;

        public InMemoryStorage(string bucketName, string objectKey)
        {
            _bucketName = bucketName;
            _objectKey = objectKey;
        }

        public Dictionary<string, StorageData> Storage = new();
        public Dictionary<string, List<StorageData>> Uploads = new();

        public InMemoryStorage Add(Stream content = null, long? size = null)
        {
            Upsert(content, size);
            return this;
        }

        private StorageData Upsert(Stream content = null, long? size = null)
        {
            var data = new StorageData(_objectKey);
            if (content != null)
                data.SetContent(content, size);
            Storage[_objectKey] = data;
            return data;
        }

        public string GetTemporaryResourceUrl(DateTime expiration)
        {
            return $"local/{_bucketName}/{_objectKey}/{expiration:yyyyMMddHHmmss}";
        }

        public Task AbortMultipartUploadAsync(string uploadId, CancellationToken cancellationToken = new())
        {
            Uploads.Remove(uploadId);
            return Task.CompletedTask;
        }

        public async Task CompleteMultiPartUploadAsync(string uploadId, List<PartETag> partTags, CancellationToken cancellationToken = new())
        {
            if (!Uploads.TryGetValue(uploadId, out var parts))
                throw new AmazonS3Exception($"The upload with id '{uploadId}' cannot be found!");

            Uploads.Remove(uploadId);

            await using var stream = new MemoryStream();
            foreach (var partETag in partTags.OrderBy(p => p.PartNumber))
            {
                var part = parts.SingleOrDefault(p => p.ETag == partETag.ETag);
                if (part == null)
                    throw new AmazonS3Exception($"The part [ETag: {partETag.ETag}, PartNumber: {partETag.PartNumber}] cannot be found.");
                await part.Content().CopyToAsync(stream, cancellationToken);
            }

            stream.Position = 0;
            Upsert(stream);
        }

        public Task<string> InitMultiPartUploadAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = new())
        {
            var uploadId = Guid.NewGuid().ToString("N")[..8];
            Uploads.Add(uploadId, new List<StorageData>());
            return Task.FromResult(uploadId);
        }

        public async Task PutAsync(Stream inputStream, CancellationToken cancellationToken = new())
        {
            var stream = new MemoryStream();
            await inputStream.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            Upsert(stream);
        }
        public async Task UploadAsync(Stream inputStream, CancellationToken cancellationToken = new())
        {
            await PutAsync(inputStream, cancellationToken);
        }
        public Task<PartETag> UploadPartAsync(string uploadId, int partNumber, int partSize, Stream inputStream, CancellationToken cancellationToken = new())
        {
            if (!Uploads.TryGetValue(uploadId, out var parts))
                throw new AmazonS3Exception($"The upload with id '{uploadId}' cannot be found!");

            var part = new StorageData(_objectKey);
            part.SetContent(inputStream, partSize);
            parts.Add(part);

            return Task.FromResult(new PartETag
            {
                ETag = part.ETag,
                PartNumber = partNumber
            });
        }
    }
}
