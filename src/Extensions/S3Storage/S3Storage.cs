// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Credentials;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Diagnostics.Monitoring.Extension.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    internal sealed class S3Storage : IS3Storage
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _objectId;
        private readonly string _contentType;
        private readonly bool _useKmsEncryption;
        private readonly string? _kmsEncryptionKey;
        private readonly bool _disablePayloadSigning;

        public S3Storage(IAmazonS3 client, string bucketName, string objectId, string contentType, bool useKmsEncryption, string? kmsEncryptionKey, bool disablePayloadSigning)
        {
            _s3Client = client;
            _bucketName = bucketName;
            _objectId = objectId;
            _contentType = contentType;
            _useKmsEncryption = useKmsEncryption;
            _kmsEncryptionKey = kmsEncryptionKey;
            _disablePayloadSigning = disablePayloadSigning;
        }

        /// <summary>
        /// Builds the <see cref="AmazonS3Config"/> describing which endpoint the client should talk to.
        /// </summary>
        /// <remarks>
        /// This is applied regardless of how the credentials are obtained: the endpoint of the storage
        /// service is orthogonal to the way the caller authenticates against it.
        /// </remarks>
        internal static AmazonS3Config CreateConfiguration(S3StorageEgressProviderOptions options)
        {
            AmazonS3Config configuration = new()
            {
                ForcePathStyle = options.ForcePathStyle
            };

            // Flexible checksums are sent as an aws-chunked trailer, which is a separate wire feature
            // from payload signing: leaving them on would still produce a STREAMING-...-TRAILER request
            // that an endpoint without chunked-payload support rejects. Opting out of payload signing
            // therefore has to opt out of request checksums as well, or the option cannot do its job.
            if (options.DisablePayloadSigning)
            {
                configuration.RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED;
                configuration.ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED;
            }

            if (!string.IsNullOrEmpty(options.Endpoint))
                configuration.ServiceURL = options.Endpoint;

            if (!string.IsNullOrEmpty(options.RegionName))
            {
                if (string.IsNullOrEmpty(configuration.ServiceURL))
                {
                    configuration.RegionEndpoint = RegionEndpoint.GetBySystemName(options.RegionName);
                }
                else
                {
                    configuration.AuthenticationRegion = options.RegionName;
                }
            }

            return configuration;
        }

        /// <summary>
        /// Resolves the credentials used to authenticate against the storage service.
        /// </summary>
        internal static AWSCredentials CreateCredentials(S3StorageEgressProviderOptions options)
        {
            AWSCredentials? awsCredentials = null;

            // use the specified access key and the secrets taken from configuration
            if (!string.IsNullOrEmpty(options.AccessKeyId) && !string.IsNullOrEmpty(options.SecretAccessKey))
            {
                string secretAccessKeyId = options.SecretAccessKey;

                // A session token indicates temporary (STS-style) credentials, which must be signed
                // with the token in addition to the access key id and secret access key.
                awsCredentials = string.IsNullOrEmpty(options.SessionToken)
                    ? new BasicAWSCredentials(options.AccessKeyId, secretAccessKeyId)
                    : new SessionAWSCredentials(options.AccessKeyId, secretAccessKeyId, options.SessionToken);
            }
            // use configured AWS profile
            else if (!string.IsNullOrEmpty(options.AwsProfileName))
            {
                CredentialProfileStoreChain chain = !string.IsNullOrEmpty(options.AwsProfilePath)
                ? new CredentialProfileStoreChain(options.AwsProfilePath)
                    : new CredentialProfileStoreChain();

                if (!chain.TryGetAWSCredentials(options.AwsProfileName, out awsCredentials))
                    throw new AmazonClientException("AWS profile not found");
            }

            awsCredentials ??= DefaultAWSCredentialsIdentityResolver.GetCredentials();

            if (awsCredentials == null)
                throw new AmazonClientException("Failed to find AWS Credentials for constructing AWS service client");

            return awsCredentials;
        }

        public static async Task<IS3Storage> CreateAsync(S3StorageEgressProviderOptions options, EgressArtifactSettings settings, CancellationToken cancellationToken)
        {
            AWSCredentials awsCredentials = CreateCredentials(options);
            AmazonS3Config configuration = CreateConfiguration(options);

            IAmazonS3 s3Client = new AmazonS3Client(awsCredentials, configuration);
            bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, options.BucketName);
            if (!exists)
                await s3Client.PutBucketAsync(options.BucketName, cancellationToken);
            return new S3Storage(s3Client, options.BucketName, settings.Name, settings.ContentType, options.UseKmsEncryption, options.KmsEncryptionKey, options.DisablePayloadSigning);
        }

        public async Task PutAsync(Stream inputStream, CancellationToken token)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                ContentType = _contentType,
                Key = _objectId,
                InputStream = inputStream,
                AutoCloseStream = false,
                DisablePayloadSigning = _disablePayloadSigning,
            };

            if (_useKmsEncryption)
            {
                request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
                if (!string.IsNullOrEmpty(_kmsEncryptionKey))
                {
                    request.ServerSideEncryptionKeyManagementServiceKeyId = _kmsEncryptionKey;
                }
            }

            await _s3Client.PutObjectAsync(request, token);
        }

        public async Task UploadAsync(Stream inputStream, CancellationToken token)
        {
            using TransferUtility transferUtility = new(_s3Client);
            TransferUtilityUploadRequest request = new()
            {
                BucketName = _bucketName,
                Key = _objectId,
                InputStream = inputStream,
                DisablePayloadSigning = _disablePayloadSigning,
            };
            await transferUtility.UploadAsync(request, token);
        }

        public async Task<string> InitMultiPartUploadAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken)
        {
            var request = new InitiateMultipartUploadRequest { BucketName = _bucketName, Key = _objectId, ContentType = _contentType };

            if (_useKmsEncryption)
            {
                request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
                if (!string.IsNullOrEmpty(_kmsEncryptionKey))
                {
                    request.ServerSideEncryptionKeyManagementServiceKeyId = _kmsEncryptionKey;
                }
            }

            foreach (var metaData in metadata)
                request.Metadata[metaData.Key] = metaData.Value;
            var response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);
            return response.UploadId;
        }

        public async Task CompleteMultiPartUploadAsync(string uploadId, List<PartETag> parts, CancellationToken cancellationToken)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = _objectId,
                UploadId = uploadId,
                PartETags = parts
            };
            await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);
        }

        public async Task AbortMultipartUploadAsync(string uploadId, CancellationToken cancellationToken)
        {
            var request = new AbortMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = _objectId,
                UploadId = uploadId
            };
            await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);
        }

        public async Task<PartETag> UploadPartAsync(string uploadId, int partNumber, int partSize, Stream inputStream, CancellationToken token)
        {
            var uploadRequest = new UploadPartRequest
            {
                BucketName = _bucketName,
                Key = _objectId,
                InputStream = inputStream,
                PartSize = partSize,
                UploadId = uploadId,
                PartNumber = partNumber,
                DisablePayloadSigning = _disablePayloadSigning,
            };
            var response = await _s3Client.UploadPartAsync(uploadRequest, token);
            return new PartETag(response.PartNumber ?? partNumber, response.ETag);
        }

        public string GetTemporaryResourceUrl(DateTime expires)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = _objectId,
                Expires = expires
            };
            string resourceId = _s3Client.GetPreSignedURL(request);
            return resourceId;
        }
    }
}
