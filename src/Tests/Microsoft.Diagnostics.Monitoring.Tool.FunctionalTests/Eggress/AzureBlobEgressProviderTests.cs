// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Azure.Storage.Blobs;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System;
using Azure.Storage.Sas;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Eggress
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(AzuriteCollectionFixture.Name)]
    public class AzureBlobEgressProviderTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly AzuriteFixture _azuriteFixture;

        public AzureBlobEgressProviderTests(ITestOutputHelper outputHelper, AzuriteFixture azuriteFixture)
        {
            _outputHelper = outputHelper;
            _azuriteFixture = azuriteFixture;
        }

        [ConditionalFact]
        public async Task RestrictiveSasTokenTest()
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TestTimeouts.OperationTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Arrange
            _azuriteFixture.SkipTestIfNotAvailable();

            BlobServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            string containerName = Guid.NewGuid().ToString();
            BlobContainerClient containerClient = await serviceClient.CreateBlobContainerAsync(containerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobSasBuilder sasBuilder = new(BlobContainerSasPermissions.Add, DateTimeOffset.MaxValue);
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            // Act

            // Assert
            var foo = await containerClient.GetBlobsAsync(cancellationToken: cancellationToken);
            Assert.True(true);
        }
    }
}
