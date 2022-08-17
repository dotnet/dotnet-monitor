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

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Eggress
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(AzuriteCollectionFixture.Name)]
    public class AzuriteTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly AzuriteFixture _azuriteFixture;

        public AzuriteTests(ITestOutputHelper outputHelper, AzuriteFixture azuriteFixture)
        {
            _outputHelper = outputHelper;
            _azuriteFixture = azuriteFixture;
        }

        [ConditionalFact]
        public async Task FixtureTest()
        {
            _azuriteFixture.SkipTestIfNotInitialized();

            BlobServiceClient serviceClient = new(_azuriteFixture.Account.ConnectionString);

            string containerName = Guid.NewGuid().ToString();
            BlobContainerClient containerClient = await serviceClient.CreateBlobContainerAsync(containerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobSasBuilder sasBuilder = new(BlobContainerSasPermissions.Add, DateTimeOffset.MaxValue);
            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            await Task.Delay(10 * 1000);
            Assert.True(true);
        }
    }
}
