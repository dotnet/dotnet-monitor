using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3
{
    /// <summary>
    /// Egress provider for egressing stream data to the s3 storage.
    /// </summary>
    internal class S3StorageEgressProvider : EgressProvider<S3StorageEgressProviderOptions>
    {
        public S3StorageEgressProvider(ILogger<S3StorageEgressProvider> logger) : base(logger)
        {
        }

        public override async Task<string> EgressAsync(
            S3StorageEgressProviderOptions options,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.S3Storage);
                await using var stream = await action(token);
                var resourceId = await UploadToStorageAsync(options, stream, artifactSettings, token);
                Logger?.EgressProviderSavedStream(EgressProviderTypes.S3Storage, resourceId);
                return resourceId;
            }
            catch (EgressException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
        }

        public override async Task<string> EgressAsync(
            S3StorageEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                var client = await CreateClientAsync(options, token);
                var uploadId = await InitMultiPartUploadAsync(client, options.BucketName, artifactSettings, token);
                await using var stream = new MultiPartUploadStream(client, options.BucketName, artifactSettings.Name, uploadId, options.CopyBufferSize!.Value);
                
                Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.S3Storage);
                await action(stream, token);
                await stream.FinalizeAsync(token); // force to push the last part
                stream.Close();

                if (stream.Parts.Count == 0)
                {
                    Logger?.LogDebug(Strings.Message_EgressS3NoContent);
                    return string.Empty; // nothing to return here
                }

                await CompleteMultiPartUploadAsync(client, options.BucketName, uploadId, artifactSettings, stream.Parts, token);

                var resourceId = client.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName = options.BucketName, Key = artifactSettings.Name, Expires = DateTime.UtcNow.AddHours(1) });
                Logger?.EgressProviderSavedStream(EgressProviderTypes.S3Storage, resourceId);
                return resourceId;
            }
            catch (Exception e)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
        }

        private async Task<string> UploadToStorageAsync(
            S3StorageEgressProviderOptions options,
            Stream stream,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            byte[] buffer = null;
            try
            {
                var client = await CreateClientAsync(options, token);
                if (stream.CanSeek) // use the stream directly
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = options.BucketName,
                        ContentType = artifactSettings.ContentType,
                        Key = artifactSettings.Name,
                        InputStream = stream,
                        AutoCloseStream = false,
                    };
                    await client.PutObjectAsync(request, token);
                }
                else // copy temporary to memory stream locally
                {
                    var uploadId = await InitMultiPartUploadAsync(client, options.BucketName, artifactSettings, token);
                    buffer = ArrayPool<byte>.Shared.Rent(options.CopyBufferSize!.Value);
                    var readBytes = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    var parts = new List<PartETag>();
                    while (readBytes > 0)
                    {
                        await using var memoryStream = new MemoryStream();
                        await memoryStream.WriteAsync(buffer, 0, readBytes, token);
                        memoryStream.Position = 0;

                        var uploadRequest = new UploadPartRequest
                        {
                            BucketName = options.BucketName,
                            Key = artifactSettings.Name,
                            InputStream = memoryStream,
                            PartSize = readBytes,
                            UploadId = uploadId,
                            PartNumber = parts.Count
                        };

                        // try to read more bytes to check if there is another part left
                        readBytes = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                        // in case there are more bytes, it isn't the last part
                        uploadRequest.IsLastPart = readBytes == 0;
                        var partResponse = await client.UploadPartAsync(uploadRequest, token);
                        parts.Add(new PartETag(parts.Count, partResponse.ETag));
                    }

                    if (parts.Count == 0)
                    {
                        Logger?.LogDebug(Strings.Message_EgressS3NoContent);
                        return string.Empty; // nothing to return here
                    }

                    await CompleteMultiPartUploadAsync(client, options.BucketName, uploadId, artifactSettings, parts, token);
                }

                var resourceId = client.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName = options.BucketName, Key = artifactSettings.Name, Expires = DateTime.UtcNow.AddHours(1) });
                Logger?.EgressProviderSavedStream(EgressProviderTypes.S3Storage, resourceId);
                return resourceId;
            }
            catch (Exception e)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
            finally
            {
                if (buffer != null) ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task<IAmazonS3> CreateClientAsync(S3StorageEgressProviderOptions options, CancellationToken cancellationToken)
        {
            string password;
            if (!string.IsNullOrEmpty(options.SecretsFile) && File.Exists(options.SecretsFile))
                password = await WrapException(async () => (await File.ReadAllTextAsync(options.SecretsFile, cancellationToken)).Trim());
            else if (!string.IsNullOrEmpty(options.Password))
                password = options.Password;
            else
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedMissingSecretsOrPassword));
            
            var credentials = new BasicAWSCredentials(options.UserName, password);
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                AuthenticationRegion = options.RegionName,
                ForcePathStyle = true
            };
            var client = new AmazonS3Client(credentials, config);

            await VerifyBucketExistsAsync(client, options.BucketName, cancellationToken);
            return client;
        }

        private static async Task<string> InitMultiPartUploadAsync(IAmazonS3 client, string bucketName, EgressArtifactSettings artifactSettings, CancellationToken cancellationToken)
        {
            var request = new InitiateMultipartUploadRequest {BucketName = bucketName, Key = artifactSettings.Name, ContentType = artifactSettings.ContentType};
            foreach (var metaData in artifactSettings.Metadata)
                request.Metadata[metaData.Key] = metaData.Value;
            var response = await client.InitiateMultipartUploadAsync(request, cancellationToken);
            return response.UploadId;
        }

        private static async Task CompleteMultiPartUploadAsync(IAmazonS3 client, string bucketName, string uploadId, EgressArtifactSettings artifactSettings, List<PartETag> parts, CancellationToken cancellationToken)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = artifactSettings.Name,
                UploadId = uploadId,
                PartETags = parts
            };
            await client.CompleteMultipartUploadAsync(request, cancellationToken);
        }

        private static T WrapException<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (DirectoryNotFoundException ex)
            {
                throw CreateException(ex);
            }
            catch (PathTooLongException ex)
            {
                throw CreateException(ex);
            }
            catch (IOException ex)
            {
                throw CreateException(ex);
            }
            catch (NotSupportedException ex)
            {
                throw CreateException(ex);
            }
            catch (SecurityException ex)
            {
                throw CreateException(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw CreateException(ex);
            }
        }

        private static EgressException CreateException(string message)
        {
            return new EgressException(WrapMessage(message));
        }

        private static EgressException CreateException(Exception innerException)
        {
            return new EgressException(WrapMessage(innerException.Message), innerException);
        }

        private static string WrapMessage(string innerMessage)
        {
            return !string.IsNullOrEmpty(innerMessage) 
                ? string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressFileFailedDetailed, innerMessage) 
                : Strings.ErrorMessage_EgressFileFailedGeneric;
        }

        private static async Task VerifyBucketExistsAsync(IAmazonS3 client, string bucketName, CancellationToken token)
        {
            var response = await client.ListBucketsAsync(token);
            if (!response.Buckets.Any(b => string.Equals(b.BucketName, bucketName, StringComparison.OrdinalIgnoreCase)))
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedBucketNotExists));
        }
    }
}