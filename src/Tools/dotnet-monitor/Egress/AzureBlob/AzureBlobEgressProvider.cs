﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class AzureBlobEgressProvider :
        EgressProvider<AzureBlobEgressProviderOptions>
    {
        private int BlobStorageBufferSize = 4 * 1024 * 1024;

        public AzureBlobEgressProvider(ILogger<AzureBlobEgressProvider> logger)
            : base(logger)
        {
        }

        public override async Task<string> EgressAsync(
            AzureBlobEgressProviderOptions options,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                var containerClient = await GetBlobContainerClientAsync(options, token);

                BlobClient blobClient = containerClient.GetBlobClient(GetBlobName(options, artifactSettings));

                Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.AzureBlobStorage);
                using var stream = await action(token);

                // Write blob content, headers, and metadata
                await blobClient.UploadAsync(stream, CreateHttpHeaders(artifactSettings), artifactSettings.Metadata, cancellationToken: token);

                string blobUriString = GetBlobUri(blobClient);
                Logger?.EgressProviderSavedStream(EgressProviderTypes.AzureBlobStorage, blobUriString);
                return blobUriString;
            }
            catch (AggregateException ex) when (ex.InnerException is RequestFailedException innerException)
            {
                throw CreateException(innerException);
            }
            catch (RequestFailedException ex)
            {
                throw CreateException(ex);
            }
        }

        public override async Task<string> EgressAsync(
            AzureBlobEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                var containerClient = await GetBlobContainerClientAsync(options, token);

                BlockBlobClient blobClient = containerClient.GetBlockBlobClient(GetBlobName(options, artifactSettings));

                // Write blob content

                var bloboptions = new BlockBlobOpenWriteOptions
                {
                    BufferSize = BlobStorageBufferSize,
                };
                using (Stream blobStream = await blobClient.OpenWriteAsync(overwrite: true, options: bloboptions, cancellationToken: token))
                using (AutoFlushStream flushStream = new AutoFlushStream(blobStream, BlobStorageBufferSize))
                {
                    //Azure's stream from OpenWriteAsync will do the following
                    //1. Write the data to a local buffer
                    //2. Once that buffer is full, stage the data remotely (this data is not considered valid yet)
                    //3. After 4Gi of data has been staged, the data will be commited. This can be forced earlier by flushing
                    //the stream.
                    // Since we want the data to be readily available, we automatically flush (and therefore commit) every time we fill up the buffer.
                    Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.AzureBlobStorage);
                    await action(flushStream, token);

                    await flushStream.FlushAsync(token);
                }

                // Write blob headers
                await blobClient.SetHttpHeadersAsync(CreateHttpHeaders(artifactSettings), cancellationToken: token);

                // Write blob metadata
                await blobClient.SetMetadataAsync(artifactSettings.Metadata, cancellationToken: token);

                string blobUriString = GetBlobUri(blobClient);
                Logger?.EgressProviderSavedStream(EgressProviderTypes.AzureBlobStorage, blobUriString);
                return blobUriString;
            }
            catch (AggregateException ex) when (ex.InnerException is RequestFailedException innerException)
            {
                throw CreateException(innerException);
            }
            catch (RequestFailedException ex)
            {
                throw CreateException(ex);
            }
        }

        private Uri GetAccountUri(AzureBlobEgressProviderOptions options, out string accountName)
        {
            var blobUriBuilder = new BlobUriBuilder(options.AccountUri);
            blobUriBuilder.Query = null;
            blobUriBuilder.BlobName = null;
            blobUriBuilder.BlobContainerName = null;

            accountName = blobUriBuilder.AccountName;

            return blobUriBuilder.ToUri();
        }

        private async Task<BlobContainerClient> GetBlobContainerClientAsync(AzureBlobEgressProviderOptions options, CancellationToken token)
        {
            BlobServiceClient serviceClient;
            if (!string.IsNullOrWhiteSpace(options.SharedAccessSignature))
            {
                var serviceUriBuilder = new UriBuilder(options.AccountUri)
                {
                    Query = options.SharedAccessSignature
                };

                serviceClient = new BlobServiceClient(serviceUriBuilder.Uri);
            }
            else if (!string.IsNullOrEmpty(options.AccountKey))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetAccountUri(options, out string accountName);

                StorageSharedKeyCredential credential = new StorageSharedKeyCredential(
                    accountName,
                    options.AccountKey);

                serviceClient = new BlobServiceClient(accountUri, credential);
            }
            else
            {
                throw CreateException(Strings.ErrorMessage_EgressMissingSasOrKey);
            }

            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: token);

            return containerClient;
        }

        private string GetBlobName(AzureBlobEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (string.IsNullOrEmpty(options.BlobPrefix))
            {
                return artifactSettings.Name;
            }
            else
            {
                return string.Concat(options.BlobPrefix, "/", artifactSettings.Name);
            }
        }

        private BlobHttpHeaders CreateHttpHeaders(EgressArtifactSettings artifactSettings)
        {
            BlobHttpHeaders headers = new BlobHttpHeaders();
            headers.ContentEncoding = artifactSettings.ContentEncoding;
            headers.ContentType = artifactSettings.ContentType;
            return headers;
        }

        private static string GetBlobUri(BlobBaseClient client)
        {
            // The BlobClient URI has the SAS token as the query parameter
            // Remove the SAS token before returning the URI
            UriBuilder outputBuilder = new UriBuilder(client.Uri);
            outputBuilder.Query = null;

            return outputBuilder.Uri.ToString();
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
            if (!string.IsNullOrEmpty(innerMessage))
            {
                return string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressAzureFailedDetailed, innerMessage);
            }
            else
            {
                return Strings.ErrorMessage_EgressAzureFailedGeneric;
            }
        }
    }
}
