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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Eggress.AzureBlob
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(AzuriteCollectionFixture.Name)]
    public class AzureBlobEgressProviderTests : IClassFixture<AzuriteFixture>
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly AzuriteFixture _azuriteFixture;

        public AzureBlobEgressProviderTests(ITestOutputHelper outputHelper, AzuriteFixture azuriteFixture)
        {
            _outputHelper = outputHelper;
            _azuriteFixture = azuriteFixture;
        }

        [ConditionalFact]
        public async Task AzureBlobEgress_Supports_AccountLevelSasToken()
        {
            _azuriteFixture.SkipTestIfNotAvailable();

            // Arrange
            TestOutputLoggerProvider loggerProvider = new(_outputHelper);
            AzureBlobEgressProvider egressProvider = new(loggerProvider.CreateLogger<AzureBlobEgressProvider>());

            QueueClient queueClient = await ConstructNewQueueContainerClient(create: false);
            BlobContainerClient containerClient = await ConstructNewBlobContainerClient(create: false);

            AzureBlobEgressProviderOptions providerOptions = new()
            {
                AccountUri = new Uri(_azuriteFixture.Account.BlobEndpoint),
                AccountKey = _azuriteFixture.Account.Key,
                ContainerName = containerClient.Name,
                QueueAccountUri = new Uri(_azuriteFixture.Account.QueueEndpoint),
                QueueName = queueClient.Name,
                BlobPrefix = Guid.NewGuid().ToString("D")
            };

            EgressArtifactSettings artifactSettings = new()
            {
                ContentType = ContentTypes.ApplicationOctetStream,
                Name = Guid.NewGuid().ToString("D")
            };

            artifactSettings.Metadata.Add("firstKey", Guid.NewGuid().ToString("D"));
            artifactSettings.Metadata.Add("secondKey", Guid.NewGuid().ToString("D"));

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

            BlobContainerClient containerClient = await ConstructNewBlobContainerClient();

            BlobSasBuilder sasBuilder = new(BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create, DateTimeOffset.MaxValue);
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            AzureBlobEgressProviderOptions providerOptions = new()
            {
                AccountUri = new Uri(_azuriteFixture.Account.BlobEndpoint),
                SharedAccessSignature = sasUri.Query,
                ContainerName = containerClient.Name,
                BlobPrefix = Guid.NewGuid().ToString("D")
            };

            EgressArtifactSettings artifactSettings = new()
            {
                ContentType = ContentTypes.ApplicationOctetStream,
                Name = Guid.NewGuid().ToString("D")
            };

            artifactSettings.Metadata.Add("firstKey", Guid.NewGuid().ToString("D"));
            artifactSettings.Metadata.Add("secondKey", Guid.NewGuid().ToString("D"));

            // Act
            string identifier = await egressProvider.EgressAsync(providerOptions, UploadAction, artifactSettings, CancellationToken.None);

            // Assert
            List<BlobItem> blobs = await GetAllBlobsAsync(containerClient);
            Assert.Single(blobs);

            BlobItem resultingBlob = blobs.First();
            Assert.Equal($"{providerOptions.BlobPrefix}/{artifactSettings.Name}", resultingBlob.Name);

        }

        private Task<Stream> UploadAction(CancellationToken token)
        {
            return Task.FromResult<Stream>(new FileStream(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read));
        }

        private async Task<BlobContainerClient> ConstructNewBlobContainerClient(string containerName = null, bool create = true)
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

        private async Task<QueueClient> ConstructNewQueueContainerClient(string queueName = null, bool create = true)
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

        private async Task<List<BlobItem>> GetAllBlobsAsync(BlobContainerClient containerClient)
        {
            List<BlobItem> blobs = new List<BlobItem>();

            var resultSegment = containerClient.GetBlobsAsync(BlobTraits.All).AsPages(default);
            await foreach (Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blob in blobPage.Values)
                {
                    blobs.Add(blob);
                }
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
    }
}
