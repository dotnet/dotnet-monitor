// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3;
using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    /// <summary>
    /// Egress provider for egressing stream data to the S3 storage.
    /// </summary>
    internal sealed class S3StorageEgressProvider : EgressProvider<S3StorageEgressProviderOptions>
    {
        private readonly ILogger _logger;

#pragma warning disable CA1852
        internal class StorageFactory
        {
            public virtual async Task<IS3Storage> CreateAsync(S3StorageEgressProviderOptions options, EgressArtifactSettings settings, CancellationToken cancellationToken) => await S3Storage.CreateAsync(options, settings, cancellationToken);
        }
#pragma warning restore CA1852

        internal StorageFactory ClientFactory = new();

        public S3StorageEgressProvider(ILogger<S3StorageEgressProvider> logger)
        {
            _logger = logger;
        }

        public override async Task<string> EgressAsync(
            S3StorageEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            IS3Storage? client = null;
            string? uploadId = null;
            bool uploadDone = false;
            try
            {
                client = await ClientFactory.CreateAsync(options, artifactSettings, token);
                uploadId = await client.InitMultiPartUploadAsync(artifactSettings.Metadata, token);
                await using var stream = new MultiPartUploadStream(client, options.BucketName, artifactSettings.Name, uploadId, options.CopyBufferSize);
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
                if (client != null && uploadId != null && !uploadDone)
                    await client.AbortMultipartUploadAsync(uploadId, token);
                throw CreateException(e.Message);
            }
        }

        private string GetResourceId(IS3Storage client, S3StorageEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (!options.PreSignedUrlExpiry.HasValue)
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
                : Strings.ErrorMessage_EgressS3FailedGeneric;
        }
    }
}
