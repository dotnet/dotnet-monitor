// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Sas;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Fixtures.Azurite;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Eggress.AzureBlob
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(AzuriteCollectionFixture.Name)]
    public class AzureBlobEgressProviderTests : IClassFixture<AzuriteFixture>, IDisposable
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly AzuriteFixture _azuriteFixture;
        private readonly TemporaryDirectory _tempDirectory;

        private readonly string _testUploadFile;
        private readonly string _testUploadFileHash;

        public AzureBlobEgressProviderTests(ITestOutputHelper outputHelper, AzuriteFixture azuriteFixture)
        {
            _outputHelper = outputHelper;
            _azuriteFixture = azuriteFixture;
            _tempDirectory = new TemporaryDirectory(outputHelper);

            _testUploadFile = Path.Combine(_tempDirectory.FullName, Path.GetRandomFileName());
            File.WriteAllText(_testUploadFile, "Sample Contents\n123");
            _testUploadFileHash = GetFileSHA256(_testUploadFile);
        }

        [ConditionalFact]
        public async Task AzureBlobEgress_UploadsCorrectData()
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClient(create: false);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await egressProvider.EgressAsync(providerOptions, UploadAction, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            Assert.Single(blobs);

            BlobItem resultingBlob = blobs.First();

            string downloadedFile = await DownloadBlobAsync(containerClient, resultingBlob.Name, CancellationToken.None);
            Assert.Equal(_testUploadFileHash, GetFileSHA256(downloadedFile));
        }


        [ConditionalFact]
        public async Task AzureBlobEgress_Supports_QueueMessages()
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClient(create: false);
            QueueClient queueClient = await ConstructQueueContainerClient(create: false);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient, queueClient);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await egressProvider.EgressAsync(providerOptions, UploadAction, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            List<QueueMessage> messages = await GetAllMessagesAsync(queueClient);

            Assert.Single(blobs);
            Assert.Single(messages);

            BlobItem resultingBlob = blobs.First();
            QueueMessage resultingMessage = messages.First();

            Assert.Equal($"{providerOptions.BlobPrefix}/{artifactSettings.Name}", resultingBlob.Name);
            // The queue message should be equal to the blob's name
            Assert.Equal(resultingBlob.Name, DecodeQueueMessageBody(resultingMessage.Body));
        }

        [ConditionalFact]
        public async Task AzureBlobEgress_Supports_RestrictiveSasToken()
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClient();

            BlobSasBuilder sasBuilder = new(BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create, DateTimeOffset.MaxValue);
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient, sasToken: sasUri.Query);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await egressProvider.EgressAsync(providerOptions, UploadAction, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            Assert.Single(blobs);

            BlobItem resultingBlob = blobs.First();
            Assert.Equal($"{providerOptions.BlobPrefix}/{artifactSettings.Name}", resultingBlob.Name);
        }

        [ConditionalFact]
        public async Task AzureBlobEgress_ThrowsWhen_ContainerDoesNotExistAndRestrictiveSasToken()
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClient(create: false);

            BlobSasBuilder sasBuilder = new(BlobContainerSasPermissions.Add, DateTimeOffset.MaxValue);
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient, sasToken: sasUri.Query);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act & Assert
            await Assert.ThrowsAsync<EgressException>(async () => await egressProvider.EgressAsync(providerOptions, UploadAction, artifactSettings, CancellationToken.None));
        }

        private Task<Stream> UploadAction(CancellationToken token)
        {
            return Task.FromResult<Stream>(new FileStream(_testUploadFile, FileMode.Open, FileAccess.Read));
        }

        private async Task<BlobContainerClient> ConstructBlobContainerClient(string containerName = null, bool create = true)
        {
            BlobServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            containerName ??= Guid.NewGuid().ToString("D");
            BlobContainerClient containerClient = await serviceClient.CreateBlobContainerAsync(containerName);

            if (create)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            return containerClient;
        }

        private async Task<QueueClient> ConstructQueueContainerClient(string queueName = null, bool create = true)
        {
            QueueServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            queueName ??= Guid.NewGuid().ToString("D");
            QueueClient queueClient = await serviceClient.CreateQueueAsync(queueName);

            if (create)
            {
                await queueClient.CreateIfNotExistsAsync();
            }

            return queueClient;
        }

        private EgressArtifactSettings ConstructArtifactSettings(int numberOfMetadataEntries = 2)
        {
            EgressArtifactSettings settings = new()
            {
                ContentType = ContentTypes.ApplicationOctetStream,
                Name = Guid.NewGuid().ToString("D")
            };

            for (int i = 0; i < numberOfMetadataEntries; i++)
            {
                settings.Metadata.Add($"key_{i}", Guid.NewGuid().ToString("D"));
            }

            return settings;
        }

        private AzureBlobEgressProviderOptions ConstructEgressProviderSettings(BlobContainerClient containerClient, QueueClient queueClient = null, string sasToken = null)
        {
            AzureBlobEgressProviderOptions options = new()
            {
                AccountUri = new Uri(_azuriteFixture.Account.BlobEndpoint),
                ContainerName = containerClient.Name,
                QueueAccountUri = (queueClient == null) ? null : new Uri(_azuriteFixture.Account.QueueEndpoint),
                QueueName = queueClient?.Name,
                BlobPrefix = Guid.NewGuid().ToString("D")
            };

            if (sasToken == null)
            {
                options.AccountKey = _azuriteFixture.Account.Key;
            }
            else
            {
                options.SharedAccessSignature = sasToken;
            }

            return options;
        }

        private async Task<List<BlobItem>> GetAllBlobsAsync(BlobContainerClient containerClient)
        {
            List<BlobItem> blobs = new();

            try
            {
                var resultSegment = containerClient.GetBlobsAsync(BlobTraits.All).AsPages(default);
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blob in blobPage.Values)
                    {
                        blobs.Add(blob);
                    }
                }
            }
            catch (XmlException)
            {
                // Thrown when there are no blobs
            }

            return blobs;
        }

        private async Task<List<QueueMessage>> GetAllMessagesAsync(QueueClient queueClient)
        {
            const int MaxReceiveMessages = 32;
            Response<QueueMessage[]> messages = await queueClient.ReceiveMessagesAsync(MaxReceiveMessages);
            return messages.Value.ToList();
        }

        private string DecodeQueueMessageBody(BinaryData body)
        {
            // Queue messages are either UTF-8 encoded XML, **OR**, in our case
            // UTF-8 encoded text that is then Base64 encoded and stored as a byte array.
            //
            // To decode it
            // - Use the BinaryData's built in `.ToString()` which will first re-encode the byte array as UTF-8 encoded text
            // - Base64 Decode the resulting string
            // - Re-encode the resulting byte array as UTF-8 text
            return Encoding.UTF8.GetString(Convert.FromBase64String(body.ToString()));
        }

        private async Task<string> DownloadBlobAsync(BlobContainerClient containerClient, string blobName, CancellationToken token)
        {
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            string downloadPath = Path.Combine(_tempDirectory.FullName, Path.GetRandomFileName());

            await using FileStream fs = File.OpenWrite(downloadPath);
            await blobClient.DownloadToAsync(fs, token);

            return downloadPath;
        }

        private string GetFileSHA256(string filePath)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream fileStream = File.OpenRead(filePath);
            return BitConverter.ToString(sha.ComputeHash(fileStream));
        }

        public void Dispose()
        {
            //_tempDirectory.Dispose();
        }
    }

}
