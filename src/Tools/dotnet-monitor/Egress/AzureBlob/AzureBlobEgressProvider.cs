// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
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

                string blobName = GetBlobName(options, artifactSettings);

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.AzureBlobStorage);
                using var stream = await action(token);

                // Write blob content, headers, and metadata
                await blobClient.UploadAsync(stream, CreateHttpHeaders(artifactSettings), artifactSettings.Metadata, cancellationToken: token);

                string blobUriString = GetBlobUri(blobClient);
                Logger?.EgressProviderSavedStream(EgressProviderTypes.AzureBlobStorage, blobUriString);

                if (CheckQueueEgressOptions(options))
                {
                    await EgressMessageToQueue(blobName, options, token);
                }

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
            catch (CredentialUnavailableException ex)
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

                string blobName = GetBlobName(options, artifactSettings);

                BlockBlobClient blobClient = containerClient.GetBlockBlobClient(blobName);

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

                if (CheckQueueEgressOptions(options))
                {
                    await EgressMessageToQueue(blobName, options, token);
                }

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
            catch (CredentialUnavailableException ex)
            {
                throw CreateException(ex);
            }
        }

        private bool CheckQueueEgressOptions(AzureBlobEgressProviderOptions options)
        {
            bool queueNameSet = !string.IsNullOrEmpty(options.QueueName);
            bool queueAccountUriSet = null != options.QueueAccountUri;

            if (queueNameSet ^ queueAccountUriSet)
            {
                Logger.QueueOptionsPartiallySet();
            }

            return queueNameSet && queueAccountUriSet;
        }

        private Uri GetBlobAccountUri(AzureBlobEgressProviderOptions options, out string accountName)
        {
            var blobUriBuilder = new BlobUriBuilder(options.AccountUri);
            blobUriBuilder.Query = null;
            blobUriBuilder.BlobName = null;
            blobUriBuilder.BlobContainerName = null;

            accountName = blobUriBuilder.AccountName;

            return blobUriBuilder.ToUri();
        }

        private Uri GetQueueAccountUri(AzureBlobEgressProviderOptions options, out string accountName)
        {
            var queueUriBuilder = new QueueUriBuilder(options.QueueAccountUri);

            queueUriBuilder.Query = null;
            queueUriBuilder.QueueName = null;

            accountName = queueUriBuilder.AccountName;

            return queueUriBuilder.ToUri();
        }

        private async Task EgressMessageToQueue(string blobName, AzureBlobEgressProviderOptions options, CancellationToken token)
        {
            try
            {
                QueueClient queueClient = await GetQueueClientAsync(options, token);

                if (queueClient.Exists())
                {
                    await queueClient.SendMessageAsync(blobName, cancellationToken: token);
                }
                else
                {
                    Logger.QueueDoesNotExist(options.QueueName);
                }
            }
            catch (Exception ex)
            {
                Logger.WritingMessageToQueueFailed(options.QueueName, ex);
            }
        }

        private async Task<QueueClient> GetQueueClientAsync(AzureBlobEgressProviderOptions options, CancellationToken token)
        {
            QueueClientOptions clientOptions = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            QueueServiceClient serviceClient;
            if (!string.IsNullOrWhiteSpace(options.SharedAccessSignature))
            {
                var serviceUriBuilder = new UriBuilder(options.QueueAccountUri)
                {
                    Query = options.SharedAccessSignature
                };

                serviceClient = new QueueServiceClient(serviceUriBuilder.Uri, clientOptions);
            }
            else if (!string.IsNullOrEmpty(options.AccountKey))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetQueueAccountUri(options, out string accountName);

                StorageSharedKeyCredential credential = new StorageSharedKeyCredential(
                    accountName,
                    options.AccountKey);

                serviceClient = new QueueServiceClient(accountUri, credential, clientOptions);
            }
            else if (!string.IsNullOrEmpty(options.ManagedIdentityClientId))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetQueueAccountUri(options, out _);

                DefaultAzureCredential credential = CreateManagedIdentityCredentials(options.ManagedIdentityClientId);

                serviceClient = new QueueServiceClient(accountUri, credential, clientOptions);
            }
            else
            {
                throw CreateException(Strings.ErrorMessage_EgressMissingCredentials);
            }

            QueueClient queueClient = serviceClient.GetQueueClient(options.QueueName);

            // This is done for instances where a SAS token may not have permission to create queues,
            // but is allowed to write a message out to one that already exists
            if (!await queueClient.ExistsAsync())
            {
                await queueClient.CreateIfNotExistsAsync(cancellationToken: token);
            }

            return queueClient;
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
                Uri accountUri = GetBlobAccountUri(options, out string accountName);

                StorageSharedKeyCredential credential = new StorageSharedKeyCredential(
                    accountName,
                    options.AccountKey);

                serviceClient = new BlobServiceClient(accountUri, credential);
            }
            else if (!string.IsNullOrEmpty(options.ManagedIdentityClientId))
            {
                Uri accountUri = GetBlobAccountUri(options, out _);

                DefaultAzureCredential credential = CreateManagedIdentityCredentials(options.ManagedIdentityClientId);

                serviceClient = new BlobServiceClient(accountUri, credential);
            }
            else
            {
                throw CreateException(Strings.ErrorMessage_EgressMissingCredentials);
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

        private static DefaultAzureCredential CreateManagedIdentityCredentials(string clientId)
        {
            var credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = clientId,
                    ExcludeAzureCliCredential = true,
                    ExcludeAzurePowerShellCredential = true,
                    ExcludeEnvironmentCredential = true,
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeVisualStudioCredential = true
                });

            return credential;
        }
    }
}
