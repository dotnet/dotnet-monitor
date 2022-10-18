﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Sas;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Xunit.Abstractions;

namespace UnitTests
{
    //[TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class AzureBlobEgressProviderTests : IClassFixture<AzuriteFixture>, IDisposable
    {
        public enum UploadAction
        {
            ProvideUploadStream,
            WriteToProviderStream
        }

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

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_UploadsCorrectData(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync(create: false);
            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            BlobItem resultingBlob = Assert.Single(blobs);

            string downloadedFile = await DownloadBlobAsync(containerClient, resultingBlob.Name, CancellationToken.None);
            Assert.Equal(_testUploadFileHash, GetFileSHA256(downloadedFile));
        }


        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_Supports_QueueMessages(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync(create: false);
            QueueClient queueClient = await ConstructQueueContainerClientAsync(create: false);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(containerClient, queueClient);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            List<QueueMessage> messages = await GetAllMessagesAsync(queueClient);

            ValidateQueue(blobs, messages, expectedCount: 1);
        }

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_Supports_RestrictiveSasToken(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync();

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(
                containerClient,
                sasToken: ConstructBlobContainerSasToken(containerClient));
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);

            BlobItem resultingBlob = Assert.Single(blobs);
            Assert.Equal($"{providerOptions.BlobPrefix}/{artifactSettings.Name}", resultingBlob.Name);
        }

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_Supports_RestrictiveQueueSasToken(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync();
            QueueClient queueClient = await ConstructQueueContainerClientAsync(create: true);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(
                containerClient,
                queueClient,
                queueSasToken: ConstructQueueSasToken(queueClient));
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            List<QueueMessage> messages = await GetAllMessagesAsync(queueClient);

            ValidateQueue(blobs, messages, expectedCount: 1);
        }

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_Supports_OnlyRestrictiveSasTokens(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync(create: true);
            QueueClient queueClient = await ConstructQueueContainerClientAsync(create: true);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(
                containerClient,
                queueClient,
                sasToken: ConstructBlobContainerSasToken(containerClient),
                queueSasToken: ConstructQueueSasToken(queueClient));

            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            List<QueueMessage> messages = await GetAllMessagesAsync(queueClient);

            ValidateQueue(blobs, messages, expectedCount: 1);
        }

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_ThrowsWhen_ContainerDoesNotExistAndUsingRestrictiveSasToken(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync(create: false);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(
                containerClient,
                sasToken: ConstructBlobContainerSasToken(containerClient));
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act & Assert
            await Assert.ThrowsAsync<EgressException>(async () => await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None));
        }

        [ConditionalTheory(Timeout = TestTimeouts.EgressUnitTestTimeoutMs)]
        [InlineData(UploadAction.ProvideUploadStream)]
        //[InlineData(UploadAction.WriteToProviderStream)]
        public async Task AzureBlobEgress_DoesNotThrowWhen_QueueDoesNotExistAndUsingRestrictiveQueueSasToken(UploadAction uploadAction)
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            BlobContainerClient containerClient = await ConstructBlobContainerClientAsync();
            QueueClient queueClient = await ConstructQueueContainerClientAsync(create: false);

            AzureBlobEgressProviderOptions providerOptions = ConstructEgressProviderSettings(
                containerClient,
                queueClient,
                queueSasToken: ConstructQueueSasToken(queueClient));
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // Act
            string identifier = await EgressAsync(uploadAction, egressProvider, providerOptions, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            List<QueueMessage> messages = await GetAllMessagesAsync(queueClient);

            Assert.Single(blobs);
            Assert.Empty(messages);
        }

        private Task<Stream> ProvideUploadStreamAsync(CancellationToken token)
        {
            return Task.FromResult<Stream>(new FileStream(_testUploadFile, FileMode.Open, FileAccess.Read));
        }

        private async Task WriteToEgressStreamAsync(Stream stream, CancellationToken token)
        {
            await using FileStream fs = new(_testUploadFile, FileMode.Open, FileAccess.Read);
            await fs.CopyToAsync(stream, token);
        }

        private async Task<string> EgressAsync(UploadAction uploadAction, AzureBlobEgressProvider egressProvider, AzureBlobEgressProviderOptions options, EgressArtifactSettings artifactSettings, CancellationToken token)
        {
            return uploadAction switch
            {
                UploadAction.ProvideUploadStream => await egressProvider.EgressAsync(options, ProvideUploadStreamAsync, artifactSettings, token),
                //UploadAction.WriteToProviderStream => await egressProvider.EgressAsync(options, WriteToEgressStreamAsync, artifactSettings, token),
                _ => throw new ArgumentException(nameof(uploadAction)),
            };
        }

        private async Task<BlobContainerClient> ConstructBlobContainerClientAsync(string containerName = null, bool create = true)
        {
            BlobServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            containerName ??= Guid.NewGuid().ToString("D");
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);

            if (create)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            return containerClient;
        }

        private async Task<QueueClient> ConstructQueueContainerClientAsync(string queueName = null, bool create = true)
        {
            QueueServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            queueName ??= Guid.NewGuid().ToString("D");
            QueueClient queueClient = serviceClient.GetQueueClient(queueName);

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

        private AzureBlobEgressProviderOptions ConstructEgressProviderSettings(BlobContainerClient containerClient, QueueClient queueClient = null, string sasToken = null, string queueSasToken = null)
        {
            AzureBlobEgressProviderOptions options = new()
            {
                AccountUri = new Uri(_azuriteFixture.Account.BlobEndpoint),
                ContainerName = containerClient.Name,
                BlobPrefix = Guid.NewGuid().ToString("D"),
                QueueAccountUri = (queueClient == null) ? null : new Uri(_azuriteFixture.Account.QueueEndpoint),
                QueueName = queueClient?.Name,
                QueueSharedAccessSignature = queueSasToken
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
                // Can be thrown when there are no blobs
            }

            return blobs;
        }

        private async Task<List<QueueMessage>> GetAllMessagesAsync(QueueClient queueClient)
        {
            try
            {
                const int MaxReceiveMessages = 32;
                Response<QueueMessage[]> messages = await queueClient.ReceiveMessagesAsync(MaxReceiveMessages);
                return messages.Value.ToList();
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {

            }

            return new List<QueueMessage>();
        }

        private string DecodeQueueMessageBody(BinaryData body)
        {
            // Our queue messages are UTF-8 encoded text that is then Base64 encoded and then stored as a byte array.
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

        private void ValidateQueue(List<BlobItem> blobs, List<QueueMessage> messages, int expectedCount)
        {
            Assert.Equal(expectedCount, messages.Count);
            Assert.Equal(expectedCount, blobs.Count);

            HashSet<string> blobNames = new(blobs.Select((b) => b.Name));
            foreach (QueueMessage message in messages)
            {
                Assert.Contains(DecodeQueueMessageBody(message.Body), blobNames);
            }
        }

        private string ConstructBlobContainerSasToken(BlobContainerClient containerClient)
        {
            // Requires:
            // - Add for UploadAction.ProvideUploadStream
            // - Write for UploadAction.WriteToProviderStream
            BlobSasBuilder sasBuilder = new(
                BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Write,
                DateTimeOffset.MaxValue)
            {
                StartsOn = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5))
            };
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            return sasUri.Query;
        }

        private string ConstructQueueSasToken(QueueClient queueClient)
        {
            QueueSasBuilder sasBuilder = new(QueueSasPermissions.Add, DateTimeOffset.MaxValue)
            {
                StartsOn = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5))
            };
            Uri sasUri = queueClient.GenerateSasUri(sasBuilder);

            return sasUri.Query;
        }

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }
    }

}
