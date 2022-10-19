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
using System.Globalization;
using System.Net;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage.AzureBlob
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class AzureBlobEgressProvider
    {
        private readonly string AzureBlobStorage = "AzureBlobStorage";
        protected ILogger Logger { get; }

        public AzureBlobEgressProvider(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<string> EgressAsync(
            AzureBlobEgressProviderOptions options,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                AddConfiguredMetadataAsync(options, artifactSettings);

                var containerClient = await GetBlobContainerClientAsync(options, token);

                string blobName = GetBlobName(options, artifactSettings);

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                Logger.EgressProviderInvokeStreamAction(AzureBlobStorage);
                using var stream = await action(token);

                // Write blob content, headers, and metadata
                await blobClient.UploadAsync(stream, CreateHttpHeaders(artifactSettings), cancellationToken: token);

                await SetBlobClientMetadata(blobClient, artifactSettings, token);

                string blobUriString = GetBlobUri(blobClient);
                Logger.EgressProviderSavedStream(AzureBlobStorage, blobUriString);

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
                    Logger.DuplicateKeyInMetadata(metadataPair.Key);
                }
            }

            try
            {
                // Write blob metadata
                await blobClient.SetMetadataAsync(mergedMetadata, cancellationToken: token);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is RequestFailedException)
            {
                Logger.InvalidMetadata(ex);
                await blobClient.SetMetadataAsync(artifactSettings.Metadata, cancellationToken: token);
            }
        }

        public void AddConfiguredMetadataAsync(AzureBlobEgressProviderOptions options, EgressArtifactSettings artifactSettings)
        {
            if (artifactSettings.EnvBlock.Count == 0)
            {
                Logger.EnvironmentBlockNotSupported();
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
                    Logger.EnvironmentVariableNotFound(metadataPair.Value);
                }
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
                await queueClient.SendMessageAsync(blobName, cancellationToken: token);
            }
            catch (RequestFailedException ex) when (ex.Status == ((int)HttpStatusCode.NotFound))
            {
                Logger.QueueDoesNotExist(options.QueueName);
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

        private async Task<BlobContainerClient> GetBlobContainerClientAsync(AzureBlobEgressProviderOptions options, CancellationToken token)
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
            else if (!string.IsNullOrEmpty(options.ManagedIdentityClientId))
            {
                // Remove Query in case SAS token was specified
                Uri accountUri = GetBlobAccountUri(options, out _);

                DefaultAzureCredential credential = CreateManagedIdentityCredentials(options.ManagedIdentityClientId);

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
