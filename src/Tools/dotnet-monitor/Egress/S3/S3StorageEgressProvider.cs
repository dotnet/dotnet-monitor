using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

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

                string resourceId = GetResourceId(client, options, artifactSettings);
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
                    using TransferUtility transferUtility = new(client);
                    await transferUtility.UploadAsync(stream, options.BucketName, artifactSettings.Name, token);
                }

                string resourceId = GetResourceId(client, options, artifactSettings);
                return resourceId;
            }
            catch (Exception e)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
        }

        private string GetResourceId(IAmazonS3 client, S3StorageEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (!options.GeneratePresSignedUrl)
                return $"BucketName={options.BucketName}, Key={artifactSettings.Name}";

            DateTime expires = DateTime.UtcNow.AddMinutes(options.PreSignedUrlExpiryInMinutes);
            string resourceId = client.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName = options.BucketName, Key = artifactSettings.Name, Expires = expires });
            Logger?.EgressProviderSavedStream(EgressProviderTypes.S3Storage, resourceId);
            return resourceId;
        }

        private static async Task<IAmazonS3> CreateClientAsync(S3StorageEgressProviderOptions options, CancellationToken cancellationToken)
        {
            AWSCredentials awsCredentials = null;
            AmazonS3Config configuration = new();
            // use the specified access key and the secrets taken from configuration or a local file
            if (!string.IsNullOrEmpty(options.AccessKeyId) && (!string.IsNullOrEmpty(options.SecretAccessKey) || !string.IsNullOrEmpty(options.SecretsAccessKeyFile)))
            {
                string secretAccessKeyId;
                if (!string.IsNullOrEmpty(options.SecretsAccessKeyFile) && File.Exists(options.SecretsAccessKeyFile))
                    secretAccessKeyId = await WrapException(async () => (await File.ReadAllTextAsync(options.SecretsAccessKeyFile, cancellationToken)).Trim());
                else
                    secretAccessKeyId = options.SecretAccessKey;
                awsCredentials = new BasicAWSCredentials(options.AccessKeyId, secretAccessKeyId);

                configuration.ForcePathStyle = options.ForcePathStyle;
                if (!string.IsNullOrEmpty(options.Endpoint))
                    configuration.ServiceURL = options.Endpoint;
                if (!string.IsNullOrEmpty(options.RegionName))
                    configuration.AuthenticationRegion = options.RegionName;
            }
            // use configured AWS profile
            else if (!string.IsNullOrEmpty(options.AwsProfileName))
            {
                CredentialProfileStoreChain chain = !string.IsNullOrEmpty(options.AwsProfileFilePath)
                    ? new CredentialProfileStoreChain(options.AwsProfileFilePath)
                    : new CredentialProfileStoreChain();

                if (!chain.TryGetAWSCredentials(options.AwsProfileName, out awsCredentials))
                    throw new AmazonClientException("AWS profile not found");
            }

            awsCredentials ??= FallbackCredentialsFactory.GetCredentials();

            if (awsCredentials == null)
                throw new AmazonClientException("Failed to find AWS Credentials for constructing AWS service client");

            AmazonS3Client s3Client = new(awsCredentials, configuration);
            await VerifyBucketExistsAsync(s3Client, options.BucketName, cancellationToken);
            return s3Client;
        }

        private static async Task<string> InitMultiPartUploadAsync(IAmazonS3 client, string bucketName, EgressArtifactSettings artifactSettings, CancellationToken cancellationToken)
        {
            var request = new InitiateMultipartUploadRequest { BucketName = bucketName, Key = artifactSettings.Name, ContentType = artifactSettings.ContentType };
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
            bool exists = await client.DoesS3BucketExistAsync(bucketName);
            if (exists)
                return;

            try
            {
                await client.PutBucketAsync(bucketName, token);
            }
            catch (Exception ex)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedBucketNotExistsOrCannotBeCreated, ex.Message));
            }
        }
    }
}
