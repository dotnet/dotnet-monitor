// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Egress.S3
{ 
    public class InMemoryS3 : IAmazonS3
    {
        public class StorageData
        {
            private Stream _content;
            public List<Tag> Tags { get; } = new();
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

        private readonly Dictionary<string, Dictionary<string, StorageData>> _storage = new();
        private readonly Dictionary<string, Dictionary<string, List<StorageData>>> _multiPartUploads = new();

        public IReadOnlyDictionary<string, StorageData> Bucket(string bucketName) => _storage[bucketName];
        public IReadOnlyDictionary<string, List<StorageData>> Uploads(string bucketName) => _multiPartUploads[bucketName];

        public InMemoryS3 Add(string bucketName, string key, List<Tag> tags = null, Stream content = null, long? size = null)
        {
            Upsert(bucketName, key, tags, content, size);
            return this;
        }

        private StorageData Upsert(string bucketName, string key, IReadOnlyCollection<Tag> tags = null, Stream content = null, long? size = null)
        {
            if (!_storage.TryGetValue(bucketName, out var bucket))
                _storage[bucketName] = bucket = new();
            var data = new StorageData(key);
            if (content != null)
                data.SetContent(content, size);
            if (tags != null)
                data.Tags.AddRange(tags);
            bucket[key] = data;
            return data;
        }

        public void Dispose() { }

        public string GeneratePreSignedURL(string bucketName, string objectKey, DateTime expiration, IDictionary<string, object> additionalProperties)
        {
            return $"local/{bucketName}/{objectKey}/{expiration:yyyyMMddHHmmss}";
        }

        public Task<IList<string>> GetAllObjectKeysAsync(string bucketName, string prefix, IDictionary<string, object> additionalProperties)
        {
            var bucket = VerifyBucket(bucketName);
            return Task.FromResult((IList<string>)bucket.Keys.Where(k => prefix == null || k.StartsWith(prefix)).ToList());
        }

        public async Task UploadObjectFromStreamAsync(string bucketName, string objectKey, Stream stream, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            VerifyBucket(bucketName);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            Upsert(bucketName, objectKey, content: memoryStream);
        }

        public Task DeleteAsync(string bucketName, string objectKey, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            var bucket = VerifyBucket(bucketName);
            bucket.Remove(objectKey);
            return Task.CompletedTask;
        }

        public async Task DeletesAsync(string bucketName, IEnumerable<string> objectKeys, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            foreach (var objectKey in objectKeys)
                await DeleteAsync(bucketName, objectKey, additionalProperties, cancellationToken);
        }

        public Task<Stream> GetObjectStreamAsync(string bucketName, string objectKey, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(bucketName, objectKey);
            return Task.FromResult((Stream)data.Content());
        }

        public async Task UploadObjectFromFilePathAsync(string bucketName, string objectKey, string filepath, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            await using var stream = File.OpenRead(filepath);
            await UploadObjectFromStreamAsync(bucketName, objectKey, stream, additionalProperties, cancellationToken);
        }

        public async Task DownloadToFilePathAsync(string bucketName, string objectKey, string filepath, IDictionary<string, object> additionalProperties, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(bucketName, objectKey);
            await using var stream = File.Create(filepath);
            await data.Content().CopyToAsync(stream, cancellationToken);
        }

        public Task MakeObjectPublicAsync(string bucketName, string objectKey, bool enable)
        {
            return Task.CompletedTask;
        }

        public Task EnsureBucketExistsAsync(string bucketName)
        {
            VerifyBucket(bucketName);
            return Task.CompletedTask;
        }

        public Task<bool> DoesS3BucketExistAsync(string bucketName)
        {
            return Task.FromResult(_storage.ContainsKey(bucketName));
        }

        public IClientConfig Config { get; } = new AmazonS3Config();

        public string GetPreSignedURL(GetPreSignedUrlRequest request)
        {
            return GeneratePreSignedURL(request.BucketName, request.Key, request.Expires, null);
        }

        public Task<AbortMultipartUploadResponse> AbortMultipartUploadAsync(string bucketName, string key, string uploadId, CancellationToken cancellationToken = new())
        {
            var success = _multiPartUploads.Remove($"{bucketName}/{uploadId}");
            return Task.FromResult(new AbortMultipartUploadResponse { HttpStatusCode = success ? HttpStatusCode.NoContent : HttpStatusCode.NotFound });
        }

        public async Task<AbortMultipartUploadResponse> AbortMultipartUploadAsync(AbortMultipartUploadRequest request, CancellationToken cancellationToken = new())
        {
            return await AbortMultipartUploadAsync(request.BucketName, request.Key, request.UploadId, cancellationToken);
        }

        public async Task<CompleteMultipartUploadResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadRequest request, CancellationToken cancellationToken = new())
        {
            if (!_multiPartUploads.TryGetValue($"{request.BucketName}", out var uploads) || !uploads.TryGetValue(request.UploadId, out var parts))
                throw new AmazonS3Exception($"The upload with id '{request.UploadId}' cannot be found!");

            uploads.Remove(request.UploadId);

            await using var stream = new MemoryStream();
            foreach (var partETag in request.PartETags.OrderBy(p => p.PartNumber))
            {
                var part = parts.SingleOrDefault(p => p.ETag == partETag.ETag);
                if (part == null)
                    throw new AmazonS3Exception($"The part [ETag: {partETag.ETag}, PartNumber: {partETag.PartNumber}] cannot be found.");
                await part.Content().CopyToAsync(stream, cancellationToken);
            }

            stream.Position = 0;
            var newObject = Upsert(request.BucketName, request.Key, parts[0].Tags, stream);
            return new CompleteMultipartUploadResponse { BucketName = request.BucketName, Key = request.Key, ETag = newObject.ETag };
        }

        public Task<CopyObjectResponse> CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(sourceBucket, sourceKey);
            Add(destinationBucket, destinationKey, data.Tags, data.Content(), data.Size);
            return Task.FromResult(new CopyObjectResponse());
        }

        public async Task<CopyObjectResponse> CopyObjectAsync(string sourceBucket, string sourceKey, string sourceVersionId, string destinationBucket, string destinationKey, CancellationToken cancellationToken = new())
        {
            return await CopyObjectAsync(sourceBucket, sourceKey, destinationBucket, destinationKey, cancellationToken);
        }

        public async Task<CopyObjectResponse> CopyObjectAsync(CopyObjectRequest request, CancellationToken cancellationToken = new())
        {
            return await CopyObjectAsync(request.SourceBucket, request.SourceKey, request.DestinationBucket, request.DestinationKey, cancellationToken);
        }

        public Task<DeleteBucketResponse> DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            var success = _storage.Remove(bucketName);
            return Task.FromResult(new DeleteBucketResponse { HttpStatusCode = success ? HttpStatusCode.NoContent : HttpStatusCode.NotFound });
        }

        public async Task<DeleteBucketResponse> DeleteBucketAsync(DeleteBucketRequest request, CancellationToken cancellationToken = new())
        {
            return await DeleteBucketAsync(request.BucketName, cancellationToken);
        }

        public Task<DeleteObjectResponse> DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            var bucket = VerifyBucket(bucketName);
            var success = bucket.Remove(key);
            return Task.FromResult(new DeleteObjectResponse { HttpStatusCode = success ? HttpStatusCode.NoContent : HttpStatusCode.NotFound });
        }

        public async Task<DeleteObjectResponse> DeleteObjectAsync(string bucketName, string key, string versionId, CancellationToken cancellationToken = new())
        {
            return await DeleteObjectAsync(bucketName, key, cancellationToken);
        }

        public async Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = new())
        {
            return await DeleteObjectAsync(request.BucketName, request.Key, cancellationToken);
        }

        public Task<DeleteObjectsResponse> DeleteObjectsAsync(DeleteObjectsRequest request, CancellationToken cancellationToken = new())
        {
            var bucket = VerifyBucket(request.BucketName);
            var keys = request.Objects.Select(o => o.Key).ToList();
            var deleted = keys.Select(k => new DeletedObject { DeleteMarker = bucket.Remove(k), Key = k })
                .Where(d => !request.Quiet || d.DeleteMarker)
                .ToList();

            return Task.FromResult(new DeleteObjectsResponse { DeletedObjects = deleted });
        }

        public Task<DeleteObjectTaggingResponse> DeleteObjectTaggingAsync(DeleteObjectTaggingRequest request, CancellationToken cancellationToken = new())
        {
            VerifyObject(request.BucketName, request.Key)
                .Tags.Clear();
            return Task.FromResult(new DeleteObjectTaggingResponse());
        }

        public Task<GetObjectResponse> GetObjectAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(bucketName, key);
            return Task.FromResult(new GetObjectResponse
            {
                Key = key,
                BucketName = bucketName,
                ContentLength = data.Size,
                Headers = { ContentLength = data.Size },
                ETag = data.ETag,
                TagCount = data.Tags.Count,
                ResponseStream = data.Content()
            });
        }

        public async Task<GetObjectResponse> GetObjectAsync(string bucketName, string key, string versionId, CancellationToken cancellationToken = new())
        {
            return await GetObjectAsync(bucketName, key, cancellationToken);
        }

        public async Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = new())
        {
            return await GetObjectAsync(request.BucketName, request.Key, cancellationToken);
        }

        public Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(bucketName, key);
            return Task.FromResult(new GetObjectMetadataResponse
            {
                ETag = data.ETag,
                ContentLength = data.Size,
                Headers = { ContentLength = data.Size },
                LastModified = data.LastModified
            });
        }

        public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, string versionId, CancellationToken cancellationToken = new())
        {
            return await GetObjectMetadataAsync(bucketName, key, cancellationToken);
        }

        public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(GetObjectMetadataRequest request, CancellationToken cancellationToken = new())
        {
            return await GetObjectMetadataAsync(request.BucketName, request.Key, cancellationToken);
        }

        public Task<GetObjectTaggingResponse> GetObjectTaggingAsync(GetObjectTaggingRequest request, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(request.BucketName, request.Key);
            return Task.FromResult(new GetObjectTaggingResponse
            {
                Tagging = data.Tags.Select(t => new Tag { Key = t.Key, Value = t.Value }).ToList()
            });
        }

        public Task<InitiateMultipartUploadResponse> InitiateMultipartUploadAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            var uploadId = Guid.NewGuid().ToString("N")[..8];
            var data = new StorageData(key);
            data.SetContent(new MemoryStream(), 0); // just a temporary item

            if (!_multiPartUploads.TryGetValue($"{bucketName}", out var uploads))
                _multiPartUploads[bucketName] = uploads = new Dictionary<string, List<StorageData>>();

            uploads.Add(uploadId, new List<StorageData> { data });
            return Task.FromResult(new InitiateMultipartUploadResponse
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId
            });
        }

        public async Task<InitiateMultipartUploadResponse> InitiateMultipartUploadAsync(InitiateMultipartUploadRequest request, CancellationToken cancellationToken = new())
        {
            var response = await InitiateMultipartUploadAsync(request.BucketName, request.Key, cancellationToken);
            var part = _multiPartUploads[response.BucketName][response.UploadId][0];
            part.Tags.AddRange(request.TagSet);
            return response;
        }

        public Task<ListBucketsResponse> ListBucketsAsync(CancellationToken cancellationToken = new())
        {
            return Task.FromResult(new ListBucketsResponse
            {
                Buckets = _storage.Keys.Select(bucketName => new S3Bucket { BucketName = bucketName }).ToList()
            });
        }

        public Task<ListBucketsResponse> ListBucketsAsync(ListBucketsRequest request, CancellationToken cancellationToken = new())
        {
            return Task.FromResult(new ListBucketsResponse
            {
                Buckets = _storage.Keys.Select(bucketName => new S3Bucket { BucketName = bucketName }).ToList()
            });
        }

        public async Task<ListObjectsResponse> ListObjectsAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            return await ListObjectsAsync(bucketName, null, cancellationToken);
        }

        public Task<ListObjectsResponse> ListObjectsAsync(string bucketName, string prefix, CancellationToken cancellationToken = new())
        {
            var bucket = VerifyBucket(bucketName);
            var data = bucket.Where(b => prefix == null || b.Key.StartsWith(prefix))
                .Select(b => b.Value)
                .Select(d => new S3Object { BucketName = bucketName, ETag = d.ETag, Key = d.Name, Size = d.Size, LastModified = d.LastModified })
                .ToList();

            return Task.FromResult(new ListObjectsResponse
            {
                Prefix = prefix,
                IsTruncated = false, // no support for paging yet
                S3Objects = data
            });
        }

        public async Task<ListObjectsResponse> ListObjectsAsync(ListObjectsRequest request, CancellationToken cancellationToken = new())
        {
            return await ListObjectsAsync(request.BucketName, request.Prefix, cancellationToken);
        }

        public Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken cancellationToken = new())
        {
            var maxKeys = request.MaxKeys == 0 ? 1000 : request.MaxKeys;
            var skip = 0;
            if (request.ContinuationToken != null)
            {
                var token = request.ContinuationToken.Split('/');
                if (token.Length != 2 || !int.TryParse(token[1], out skip))
                    throw new AmazonS3Exception("Invalid continuation token!");
            }

            var bucket = VerifyBucket(request.BucketName);
            var page = bucket
                .OrderBy(k => k.Key)
                .Where(b => request.Prefix == null || b.Key.StartsWith(request.Prefix))
                .Skip(skip)
                .Take(maxKeys)
                .Select(b => b.Value)
                .Select(d => new S3Object { BucketName = request.BucketName, ETag = d.ETag, Key = d.Name, Size = d.Size, LastModified = d.LastModified })
                .ToList();

            var isTruncated = page.Count == maxKeys;
            var nextToken = isTruncated ? $"{Guid.NewGuid().ToString("N")[..8]}/{skip + maxKeys}" : null;

            return Task.FromResult(new ListObjectsV2Response
            {
                Prefix = request.Prefix,
                IsTruncated = isTruncated,
                NextContinuationToken = nextToken,
                S3Objects = page
            });
        }

        public async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = new())
        {
            VerifyBucket(request.BucketName);

            var stream = new MemoryStream();
            if (request.FilePath != null)
            {
                await using var file = File.OpenRead(request.FilePath);
                await file.CopyToAsync(stream, cancellationToken);
            }
            else if (request.ContentBody != null)
            {
                var content = Encoding.UTF8.GetBytes(request.ContentBody);
                await stream.WriteAsync(content, 0, content.Length, cancellationToken);
            }
            else
            {
                await request.InputStream.CopyToAsync(stream, cancellationToken);
                if (request.AutoCloseStream)
                    request.InputStream.Close();
            }

            stream.Position = 0;
            var data = Upsert(request.BucketName, request.Key, request.TagSet, stream);
            return new PutObjectResponse { ETag = data.ETag };
        }

        public Task<PutObjectTaggingResponse> PutObjectTaggingAsync(PutObjectTaggingRequest request, CancellationToken cancellationToken = new())
        {
            var data = VerifyObject(request.BucketName, request.Key);
            data.Tags.Clear();
            data.Tags.AddRange(request.Tagging.TagSet);
            return Task.FromResult(new PutObjectTaggingResponse());
        }

        public async Task<UploadPartResponse> UploadPartAsync(UploadPartRequest request, CancellationToken cancellationToken = new())
        {
            if (!_multiPartUploads.TryGetValue($"{request.BucketName}", out var uploads) || !uploads.TryGetValue(request.UploadId, out var parts))
                throw new AmazonS3Exception($"The upload with id '{request.UploadId}' cannot be found!");

            var part = new StorageData(request.Key);
            if (request.InputStream != null)
                part.SetContent(request.InputStream, request.PartSize);
            else
            {
                await using var file = File.OpenRead(request.FilePath);
                file.Position = request.FilePosition;

                var buffer = new byte[request.PartSize];
                var read = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                var stream = new MemoryStream();
                await stream.WriteAsync(buffer, 0, read, cancellationToken);
                stream.Position = 0;
                part.SetContent(stream, request.PartSize);
            }
            parts.Add(part);

            return new UploadPartResponse
            {
                ETag = part.ETag,
                PartNumber = request.PartNumber
            };
        }

        public IS3PaginatorFactory Paginators => null;

        private IDictionary<string, StorageData> VerifyBucket(string bucketName)
        {
            if (!_storage.TryGetValue(bucketName, out var bucket))
                throw new AmazonS3Exception($"The bucket with name '{bucketName}' doesn't exist.");
            return bucket;
        }

        private StorageData VerifyObject(string bucketName, string objectKey)
        {
            if (!_storage.TryGetValue(bucketName, out var bucket))
                throw new AmazonS3Exception($"The bucket with name '{bucketName}' doesn't exist.");
            if (!bucket.TryGetValue(objectKey, out var data))
                throw new AmazonS3Exception($"The object with key '{objectKey}' was not found in bucket '{bucketName}'.");
            return data;
        }

        #region NotYetSupported


        public Task<CopyPartResponse> CopyPartAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, string uploadId, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<CopyPartResponse> CopyPartAsync(string sourceBucket, string sourceKey, string sourceVersionId, string destinationBucket, string destinationKey, string uploadId, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<CopyPartResponse> CopyPartAsync(CopyPartRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketAnalyticsConfigurationResponse> DeleteBucketAnalyticsConfigurationAsync(DeleteBucketAnalyticsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketEncryptionResponse> DeleteBucketEncryptionAsync(DeleteBucketEncryptionRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketIntelligentTieringConfigurationResponse> DeleteBucketIntelligentTieringConfigurationAsync(DeleteBucketIntelligentTieringConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketInventoryConfigurationResponse> DeleteBucketInventoryConfigurationAsync(DeleteBucketInventoryConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketMetricsConfigurationResponse> DeleteBucketMetricsConfigurationAsync(DeleteBucketMetricsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketOwnershipControlsResponse> DeleteBucketOwnershipControlsAsync(DeleteBucketOwnershipControlsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketPolicyResponse> DeleteBucketPolicyAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketPolicyResponse> DeleteBucketPolicyAsync(DeleteBucketPolicyRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketReplicationResponse> DeleteBucketReplicationAsync(DeleteBucketReplicationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketTaggingResponse> DeleteBucketTaggingAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketTaggingResponse> DeleteBucketTaggingAsync(DeleteBucketTaggingRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketWebsiteResponse> DeleteBucketWebsiteAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteBucketWebsiteResponse> DeleteBucketWebsiteAsync(DeleteBucketWebsiteRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteCORSConfigurationResponse> DeleteCORSConfigurationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteCORSConfigurationResponse> DeleteCORSConfigurationAsync(DeleteCORSConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteLifecycleConfigurationResponse> DeleteLifecycleConfigurationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeleteLifecycleConfigurationResponse> DeleteLifecycleConfigurationAsync(DeleteLifecycleConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<DeletePublicAccessBlockResponse> DeletePublicAccessBlockAsync(DeletePublicAccessBlockRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetACLResponse> GetACLAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetACLResponse> GetACLAsync(GetACLRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketAccelerateConfigurationResponse> GetBucketAccelerateConfigurationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketAccelerateConfigurationResponse> GetBucketAccelerateConfigurationAsync(GetBucketAccelerateConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketAnalyticsConfigurationResponse> GetBucketAnalyticsConfigurationAsync(GetBucketAnalyticsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketEncryptionResponse> GetBucketEncryptionAsync(GetBucketEncryptionRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketIntelligentTieringConfigurationResponse> GetBucketIntelligentTieringConfigurationAsync(GetBucketIntelligentTieringConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketInventoryConfigurationResponse> GetBucketInventoryConfigurationAsync(GetBucketInventoryConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketLocationResponse> GetBucketLocationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketLocationResponse> GetBucketLocationAsync(GetBucketLocationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketLoggingResponse> GetBucketLoggingAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketLoggingResponse> GetBucketLoggingAsync(GetBucketLoggingRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketMetricsConfigurationResponse> GetBucketMetricsConfigurationAsync(GetBucketMetricsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketNotificationResponse> GetBucketNotificationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketNotificationResponse> GetBucketNotificationAsync(GetBucketNotificationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketOwnershipControlsResponse> GetBucketOwnershipControlsAsync(GetBucketOwnershipControlsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketPolicyResponse> GetBucketPolicyAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketPolicyResponse> GetBucketPolicyAsync(GetBucketPolicyRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketPolicyStatusResponse> GetBucketPolicyStatusAsync(GetBucketPolicyStatusRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketReplicationResponse> GetBucketReplicationAsync(GetBucketReplicationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketRequestPaymentResponse> GetBucketRequestPaymentAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketRequestPaymentResponse> GetBucketRequestPaymentAsync(GetBucketRequestPaymentRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketTaggingResponse> GetBucketTaggingAsync(GetBucketTaggingRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketVersioningResponse> GetBucketVersioningAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketVersioningResponse> GetBucketVersioningAsync(GetBucketVersioningRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketWebsiteResponse> GetBucketWebsiteAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetBucketWebsiteResponse> GetBucketWebsiteAsync(GetBucketWebsiteRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetCORSConfigurationResponse> GetCORSConfigurationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetCORSConfigurationResponse> GetCORSConfigurationAsync(GetCORSConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetLifecycleConfigurationResponse> GetLifecycleConfigurationAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetLifecycleConfigurationResponse> GetLifecycleConfigurationAsync(GetLifecycleConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetObjectLegalHoldResponse> GetObjectLegalHoldAsync(GetObjectLegalHoldRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetObjectLockConfigurationResponse> GetObjectLockConfigurationAsync(GetObjectLockConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetObjectRetentionResponse> GetObjectRetentionAsync(GetObjectRetentionRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetObjectTorrentResponse> GetObjectTorrentAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetObjectTorrentResponse> GetObjectTorrentAsync(GetObjectTorrentRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<GetPublicAccessBlockResponse> GetPublicAccessBlockAsync(GetPublicAccessBlockRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListBucketAnalyticsConfigurationsResponse> ListBucketAnalyticsConfigurationsAsync(ListBucketAnalyticsConfigurationsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListBucketIntelligentTieringConfigurationsResponse> ListBucketIntelligentTieringConfigurationsAsync(ListBucketIntelligentTieringConfigurationsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListBucketInventoryConfigurationsResponse> ListBucketInventoryConfigurationsAsync(ListBucketInventoryConfigurationsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListBucketMetricsConfigurationsResponse> ListBucketMetricsConfigurationsAsync(ListBucketMetricsConfigurationsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListMultipartUploadsResponse> ListMultipartUploadsAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListMultipartUploadsResponse> ListMultipartUploadsAsync(string bucketName, string prefix, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListMultipartUploadsResponse> ListMultipartUploadsAsync(ListMultipartUploadsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListPartsResponse> ListPartsAsync(string bucketName, string key, string uploadId, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListPartsResponse> ListPartsAsync(ListPartsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListVersionsResponse> ListVersionsAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListVersionsResponse> ListVersionsAsync(string bucketName, string prefix, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<ListVersionsResponse> ListVersionsAsync(ListVersionsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutACLResponse> PutACLAsync(PutACLRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketResponse> PutBucketAsync(string bucketName, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketResponse> PutBucketAsync(PutBucketRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketAccelerateConfigurationResponse> PutBucketAccelerateConfigurationAsync(PutBucketAccelerateConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketAnalyticsConfigurationResponse> PutBucketAnalyticsConfigurationAsync(PutBucketAnalyticsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketEncryptionResponse> PutBucketEncryptionAsync(PutBucketEncryptionRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketIntelligentTieringConfigurationResponse> PutBucketIntelligentTieringConfigurationAsync(PutBucketIntelligentTieringConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketInventoryConfigurationResponse> PutBucketInventoryConfigurationAsync(PutBucketInventoryConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketLoggingResponse> PutBucketLoggingAsync(PutBucketLoggingRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketMetricsConfigurationResponse> PutBucketMetricsConfigurationAsync(PutBucketMetricsConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketNotificationResponse> PutBucketNotificationAsync(PutBucketNotificationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketOwnershipControlsResponse> PutBucketOwnershipControlsAsync(PutBucketOwnershipControlsRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketPolicyResponse> PutBucketPolicyAsync(string bucketName, string policy, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketPolicyResponse> PutBucketPolicyAsync(string bucketName, string policy, string contentMd5, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketPolicyResponse> PutBucketPolicyAsync(PutBucketPolicyRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketReplicationResponse> PutBucketReplicationAsync(PutBucketReplicationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketRequestPaymentResponse> PutBucketRequestPaymentAsync(string bucketName, RequestPaymentConfiguration requestPaymentConfiguration, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketRequestPaymentResponse> PutBucketRequestPaymentAsync(PutBucketRequestPaymentRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketTaggingResponse> PutBucketTaggingAsync(string bucketName, List<Tag> tagSet, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketTaggingResponse> PutBucketTaggingAsync(PutBucketTaggingRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketVersioningResponse> PutBucketVersioningAsync(PutBucketVersioningRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketWebsiteResponse> PutBucketWebsiteAsync(string bucketName, WebsiteConfiguration websiteConfiguration, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutBucketWebsiteResponse> PutBucketWebsiteAsync(PutBucketWebsiteRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutCORSConfigurationResponse> PutCORSConfigurationAsync(string bucketName, CORSConfiguration configuration, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutCORSConfigurationResponse> PutCORSConfigurationAsync(PutCORSConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutLifecycleConfigurationResponse> PutLifecycleConfigurationAsync(string bucketName, LifecycleConfiguration configuration, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutLifecycleConfigurationResponse> PutLifecycleConfigurationAsync(PutLifecycleConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutObjectLegalHoldResponse> PutObjectLegalHoldAsync(PutObjectLegalHoldRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutObjectLockConfigurationResponse> PutObjectLockConfigurationAsync(PutObjectLockConfigurationRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutObjectRetentionResponse> PutObjectRetentionAsync(PutObjectRetentionRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<PutPublicAccessBlockResponse> PutPublicAccessBlockAsync(PutPublicAccessBlockRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<RestoreObjectResponse> RestoreObjectAsync(string bucketName, string key, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<RestoreObjectResponse> RestoreObjectAsync(string bucketName, string key, int days, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<RestoreObjectResponse> RestoreObjectAsync(string bucketName, string key, string versionId, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<RestoreObjectResponse> RestoreObjectAsync(string bucketName, string key, string versionId, int days, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<RestoreObjectResponse> RestoreObjectAsync(RestoreObjectRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        public Task<SelectObjectContentResponse> SelectObjectContentAsync(SelectObjectContentRequest request, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
