// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class AzureBlobEgressProvider : EgressProvider<AzureBlobEgressProviderOptions>
    {
        private int DefaultBlobStorageBufferSize = 4 * 1024 * 1024;

        private readonly ILogger _logger;

        public AzureBlobEgressProvider(ILogger<AzureBlobEgressProvider> logger)
        {
            _logger = logger;
        }

        public override async Task<string> EgressAsync(
            AzureBlobEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                AddConfiguredMetadataAsync(options, artifactSettings);

                var containerClient = await GetBlobContainerClientAsync(options, token);

                string blobName = GetBlobName(options, artifactSettings);

                BlockBlobClient blobClient = containerClient.GetBlockBlobClient(blobName);

                // Write blob content

                var bloboptions = new BlockBlobOpenWriteOptions
                {
                    BufferSize = options.CopyBufferSize.GetValueOrDefault(DefaultBlobStorageBufferSize),
                };
                using (Stream blobStream = await blobClient.OpenWriteAsync(overwrite: true, options: bloboptions, cancellationToken: token))
                using (AutoFlushStream flushStream = new AutoFlushStream(blobStream, bloboptions.BufferSize.Value))
                {
                    //Azure's stream from OpenWriteAsync will do the following
                    //1. Write the data to a local buffer
                    //2. Once that buffer is full, stage the data remotely (this data is not considered valid yet)
                    //3. After 4Gi of data has been staged, the data will be committed. This can be forced earlier by flushing
                    //the stream.
                    // Since we want the data to be readily available, we automatically flush (and therefore commit) every time we fill up the buffer.
                    _logger.EgressProviderInvokeStreamAction(Constants.AzureBlobStorageProviderName);
                    await action(flushStream, token);

                    await flushStream.FlushAsync(token);
                }

                // Write blob headers
                await blobClient.SetHttpHeadersAsync(CreateHttpHeaders(artifactSettings), cancellationToken: token);

                await SetBlobClientMetadata(blobClient, artifactSettings, token);

                string blobUriString = GetBlobUri(blobClient);
                _logger.EgressProviderSavedStream(Constants.AzureBlobStorageProviderName, blobUriString);

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

        public async Task SetBlobClientMetadata(BlobBaseClient blobClient, EgressArtifactSettings artifactSettings, CancellationToken token)
        {
            Dictionary<string, string> mergedMetadata = new Dictionary<string, string>(artifactSettings.Metadata);

            foreach (var metadataPair in artifactSettings.CustomMetadata)
            {
                if (!mergedMetadata.ContainsKey(metadataPair.Key))
                {
                    mergedMetadata[metadataPair.Key] = metadataPair.Value;
                }
                else
                {
                    _logger.DuplicateKeyInMetadata(metadataPair.Key);
                }
            }

            try
            {
                // Write blob metadata
                await blobClient.SetMetadataAsync(mergedMetadata, cancellationToken: token);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
            {
                _logger.InvalidMetadata(ex);
                await blobClient.SetMetadataAsync(artifactSettings.Metadata, cancellationToken: token);
            }
        }

        public void AddConfiguredMetadataAsync(AzureBlobEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (artifactSettings.EnvBlock.Count == 0)
            {
                _logger.EnvironmentBlockNotSupported();
                return;
            }

            foreach (var metadataPair in options.Metadata)
            {
                if (artifactSettings.EnvBlock.TryGetValue(metadataPair.Value, out string envVarValue))
                {
                    artifactSettings.CustomMetadata.Add(metadataPair.Key, envVarValue);
                }
                else
                {
                    _logger.EnvironmentVariableNotFound(metadataPair.Value);
                }
            }
        }

        private bool CheckQueueEgressOptions(AzureBlobEgressProviderOptions options)
        {
            bool queueNameSet = !string.IsNullOrEmpty(options.QueueName);
            bool queueAccountUriSet = null != options.QueueAccountUri;

            if (queueNameSet ^ queueAccountUriSet)
            {
                _logger.QueueOptionsPartiallySet();
            }

            return queueNameSet && queueAccountUriSet;
        }

        private static Uri GetBlobAccountUri(AzureBlobEgressProviderOptions options, out string accountName)
        {
            var blobUriBuilder = new BlobUriBuilder(options.AccountUri);
            blobUriBuilder.Query = null;
            blobUriBuilder.BlobName = null;
            blobUriBuilder.BlobContainerName = null;

            accountName = blobUriBuilder.AccountName;

            return blobUriBuilder.ToUri();
        }

        private static Uri GetQueueAccountUri(AzureBlobEgressProviderOptions options, out string accountName)
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
                await queueClient.SendMessageAsync(blobName, cancellationToken: token);
            }
            catch (RequestFailedException ex) when (ex.Status == ((int)HttpStatusCode.NotFound))
            {
                _logger.QueueDoesNotExist(options.QueueName);
            }
            catch (Exception ex)
            {
                _logger.WritingMessageToQueueFailed(options.QueueName, ex);
            }
        }

        private static async Task<QueueClient> GetQueueClientAsync(AzureBlobEgressProviderOptions options, CancellationToken token)
        {
            QueueClientOptions clientOptions = new()
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            QueueServiceClient serviceClient;
            bool mayHaveLimitedPermissions = false;
            if (!string.IsNullOrWhiteSpace(options.QueueSharedAccessSignature))
            {
                var serviceUriBuilder = new UriBuilder(options.QueueAccountUri)
                {
                    Query = options.QueueSharedAccessSignature
                };

                serviceClient = new QueueServiceClient(serviceUriBuilder.Uri, clientOptions);
                mayHaveLimitedPermissions = true;
            }
            else if (!string.IsNullOrWhiteSpace(options.SharedAccessSignature))
            {
                var serviceUriBuilder = new UriBuilder(options.QueueAccountUri)
                {
                    Query = options.SharedAccessSignature
                };

                serviceClient = new QueueServiceClient(serviceUriBuilder.Uri, clientOptions);
                mayHaveLimitedPermissions = true;
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
            else if (UseDefaultCredentials(options))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetQueueAccountUri(options, out _);

                TokenCredential credential = CreateDefaultCredential(options);

                serviceClient = new QueueServiceClient(accountUri, credential, clientOptions);
            }
            else
            {
                throw CreateException(Strings.ErrorMessage_EgressMissingCredentials);
            }

            QueueClient queueClient = serviceClient.GetQueueClient(options.QueueName);

            try
            {
                await queueClient.CreateIfNotExistsAsync(cancellationToken: token);
            }
            catch (RequestFailedException ex) when (mayHaveLimitedPermissions && ex.Status == ((int)HttpStatusCode.Forbidden))
            {
                // Ignore forbidden exceptions from trying to ensure the queue exists when dealing with potentially restrictive permissions
                // as checking if a queue exists requires account-level access.
            }

            return queueClient;
        }

        private static async Task<BlobContainerClient> GetBlobContainerClientAsync(AzureBlobEgressProviderOptions options, CancellationToken token)
        {
            bool mayHaveLimitedPermissions = false;
            BlobServiceClient serviceClient;
            if (!string.IsNullOrWhiteSpace(options.SharedAccessSignature))
            {
                var serviceUriBuilder = new UriBuilder(options.AccountUri)
                {
                    Query = options.SharedAccessSignature
                };

                serviceClient = new BlobServiceClient(serviceUriBuilder.Uri);
                mayHaveLimitedPermissions = true;
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
            else if (UseDefaultCredentials(options))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetBlobAccountUri(options, out _);

                TokenCredential credential = CreateDefaultCredential(options);

                serviceClient = new BlobServiceClient(accountUri, credential);
            }
            else
            {
                throw CreateException(Strings.ErrorMessage_EgressMissingCredentials);
            }

            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(options.ContainerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: token);
            }
            catch (RequestFailedException ex) when (mayHaveLimitedPermissions && ex.Status == ((int)HttpStatusCode.Forbidden))
            {
                // Ignore forbidden exceptions from trying to ensure the blob container exists when dealing with potentially restrictive permissions
                // as checking if a container exists requires account-level access.
            }

            return containerClient;
        }

        private static string GetBlobName(AzureBlobEgressProviderOptions options, EgressArtifactSettings artifactSettings)
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

        private static BlobHttpHeaders CreateHttpHeaders(EgressArtifactSettings artifactSettings)
        {
            BlobHttpHeaders headers = new BlobHttpHeaders();
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

        private static bool UseDefaultCredentials(AzureBlobEgressProviderOptions options) =>
            !string.IsNullOrEmpty(options.ManagedIdentityClientId) || options.UseWorkloadIdentityFromEnvironment == true;

        private static TokenCredential CreateDefaultCredential(AzureBlobEgressProviderOptions options)
        {
            DefaultAzureCredentialOptions credOptions = GetDefaultCredentialOptions();

            if (options.UseWorkloadIdentityFromEnvironment == true)
            {
                credOptions.ExcludeWorkloadIdentityCredential = false;
            }

            if (!string.IsNullOrEmpty(options.ManagedIdentityClientId))
            {
                credOptions.ExcludeManagedIdentityCredential = false;
                credOptions.ManagedIdentityClientId = options.ManagedIdentityClientId;
            }

            return new DefaultAzureCredential(credOptions);
        }

        private static DefaultAzureCredentialOptions GetDefaultCredentialOptions() =>
            new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
            };
    }
}
