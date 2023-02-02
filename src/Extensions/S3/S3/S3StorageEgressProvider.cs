// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.S3
{
    /// <summary>
    /// Egress provider for egressing stream data to the S3 storage.
    /// </summary>
    internal sealed class S3StorageEgressProvider
    {
#pragma warning disable CA1852
        internal class StorageFactory
        {
            public virtual async Task<IS3Storage> CreateAsync(S3StorageEgressProviderOptions options, EgressArtifactSettings settings, CancellationToken cancellationToken) => await S3Storage.CreateAsync(options, settings, cancellationToken);
        }
#pragma warning restore CA1852

        internal StorageFactory ClientFactory = new();
        private readonly ILogger _logger;

        public S3StorageEgressProvider(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> EgressAsync(
            S3StorageEgressProviderOptions options,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                _logger.EgressProviderInvokeStreamAction(Constants.S3StorageProviderName);
                await using var stream = await action(token);

                var client = await ClientFactory.CreateAsync(options, artifactSettings, token);
                if (stream.CanSeek) // use the stream directly
                {
                    await client.PutAsync(stream, token);
                }
                else // copy temporary to memory stream locally
                {
                    await client.UploadAsync(stream, token);
                }

                string resourceId = GetResourceId(client, options, artifactSettings);
                return resourceId;
            }
            catch (AmazonS3Exception e)
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
        }

        public async Task<string> EgressAsync(
            S3StorageEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            IS3Storage client = null;
            string uploadId = null;
            bool uploadDone = false;
            try
            {
                client = await ClientFactory.CreateAsync(options, artifactSettings, token);
                uploadId = await client.InitMultiPartUploadAsync(artifactSettings.Metadata, token);
                int copyBufferSize = options.CopyBufferSize.GetValueOrDefault(0x100000);
                await using var stream = new MultiPartUploadStream(client, options.BucketName, artifactSettings.Name, uploadId, copyBufferSize);
                _logger.EgressProviderInvokeStreamAction(Constants.S3StorageProviderName);
                await action(stream, token);
                await stream.FinalizeAsync(token); // force to push the last part

                // an empty file was generated
                if (stream.Parts.Count == 0)
                {
                    await client.PutAsync(new MemoryStream(), token);
                    uploadDone = true;
                    // abort the multi-part upload
                    await client.AbortMultipartUploadAsync(uploadId, token);
                }
                else
                {
                    await client.CompleteMultiPartUploadAsync(uploadId, stream.Parts, token);
                    uploadDone = true;
                }

                string resourceId = GetResourceId(client, options, artifactSettings);
                return resourceId;
            }
            catch (AmazonS3Exception e)
            {
                if (!uploadDone)
                    await client.AbortMultipartUploadAsync(uploadId, token);
                throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, e.Message));
            }
        }

        private string GetResourceId(IS3Storage client, S3StorageEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (!options.GeneratePreSignedUrl)
                return $"BucketName={options.BucketName}, Key={artifactSettings.Name}";

            DateTime expires = DateTime.UtcNow.Add(options.PreSignedUrlExpiry!.Value);
            string resourceId = client.GetTemporaryResourceUrl(expires);
            _logger.EgressProviderSavedStream(Constants.S3StorageProviderName, resourceId);
            return resourceId;
        }

        private static EgressException CreateException(string message)
        {
            return new EgressException(WrapMessage(message));
        }

        private static string WrapMessage(string innerMessage)
        {
            return !string.IsNullOrEmpty(innerMessage)
                ? string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressS3FailedDetailed, innerMessage)
                : Strings.ErrorMessage_EgressFileFailedGeneric;
        }
    }
}
