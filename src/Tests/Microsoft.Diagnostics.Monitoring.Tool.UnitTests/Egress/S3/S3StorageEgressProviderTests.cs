﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Amazon.S3;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.S3;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Egress.S3
{
    public class S3StorageEgressProviderTests
    {
        public enum EUploadAction
        {
            ProvideUploadStream,
            WriteToProviderStream
        }

        internal sealed class InMemoryS3ClientFactory : S3StorageEgressProvider.StorageFactory
        {
            public InMemoryStorage S3;
            public override Task<IS3Storage> CreateAsync(S3StorageEgressProviderOptions options, EgressArtifactSettings settings, CancellationToken token)
            {
                S3 = new InMemoryStorage(options.BucketName, settings.Name);
                return Task.FromResult((IS3Storage)S3);
            }
        }

        private readonly TestOutputLoggerProvider _loggerProvider;

        public S3StorageEgressProviderTests(ITestOutputHelper outputHelper)
        {
            // Arrange
            _loggerProvider = new(outputHelper);
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadFile(EUploadAction uploadAction)
        {
            var clientFactory = new InMemoryS3ClientFactory();
            var sut = new S3StorageEgressProvider(_loggerProvider.CreateLogger<S3StorageEgressProvider>()) { ClientFactory = clientFactory };

            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = MultiPartUploadStream.MinimumSize * 3 + 1024;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await sut.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await sut.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException("Unsupported value", nameof(uploadAction))
            };

            // verify
            Assert.Equal($"BucketName={options.BucketName}, Key={artifactSettings.Name}", resourceId);

            var storage = clientFactory.S3;
            (string key, InMemoryStorage.StorageData data) = Assert.Single(storage.Storage);
            Assert.Equal(key, artifactSettings.Name);
            Assert.Equal(totalBytes, data.Size);
            Assert.Equal(stream.ToArray(), data.Bytes());
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadEmptyFile(EUploadAction uploadAction)
        {
            var clientFactory = new InMemoryS3ClientFactory();
            var sut = new S3StorageEgressProvider(_loggerProvider.CreateLogger<S3StorageEgressProvider>()) { ClientFactory = clientFactory };

            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = 0;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await sut.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await sut.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException("Unsupported value", nameof(uploadAction))
            };

            // verify
            Assert.Equal($"BucketName={options.BucketName}, Key={artifactSettings.Name}", resourceId);

            var storage = clientFactory.S3;
            (string key, InMemoryStorage.StorageData data) = Assert.Single(storage.Storage);
            Assert.Equal(key, artifactSettings.Name);
            Assert.Equal(totalBytes, data.Size);
            Assert.Equal(stream.ToArray(), data.Bytes());

            if (uploadAction == EUploadAction.WriteToProviderStream)
                Assert.Empty(storage.Uploads); // the upload should be aborted
        }

        [Fact]
        public async Task ItShouldAbortOnError()
        {
            var clientFactory = new InMemoryS3ClientFactory();
            var sut = new S3StorageEgressProvider(_loggerProvider.CreateLogger<S3StorageEgressProvider>()) { ClientFactory = clientFactory };

            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            await Assert.ThrowsAnyAsync<Exception>(async () => await sut.EgressAsync(options, (stream, token) => throw new AmazonS3Exception(new Exception()), artifactSettings, CancellationToken.None));

            var storage = clientFactory.S3;
            Assert.Empty(storage.Storage);
            Assert.Empty(storage.Uploads); // the upload should be aborted
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadFileAndGeneratePreSignedUrl(EUploadAction uploadAction)
        {
            var clientFactory = new InMemoryS3ClientFactory();
            var sut = new S3StorageEgressProvider(_loggerProvider.CreateLogger<S3StorageEgressProvider>()) { ClientFactory = clientFactory };

            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            options.GeneratePreSignedUrl = true;
            options.PreSignedUrlExpiry = TimeSpan.FromMinutes(10);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = MultiPartUploadStream.MinimumSize * 3 + 1024;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await sut.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await sut.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException("Unsupported value", nameof(uploadAction))
            };

            // verify
            var expiration = DateTime.UtcNow.Add(options.PreSignedUrlExpiry!.Value);
            var expectation = $"local/{options.BucketName}/{artifactSettings.Name}/{expiration:yyyyMMddHH}";
            Assert.StartsWith(expectation, resourceId);

            var storage = clientFactory.S3;
            (string key, InMemoryStorage.StorageData data) = Assert.Single(storage.Storage);
            Assert.Equal(key, artifactSettings.Name);
            Assert.Equal(totalBytes, data.Size);
            Assert.Equal(stream.ToArray(), data.Bytes());
        }

        private static EgressArtifactSettings ConstructArtifactSettings(int numberOfMetadataEntries = 2)
        {
            EgressArtifactSettings settings = new()
            {
                ContentType = ContentTypes.ApplicationOctetStream,
                Name = Guid.NewGuid().ToString("N")
            };

            for (int i = 0; i < numberOfMetadataEntries; i++)
            {
                settings.Metadata.Add($"key_{i}", Guid.NewGuid().ToString("D"));
            }

            return settings;
        }

        private static S3StorageEgressProviderOptions ConstructEgressProviderSettings()
        {
            S3StorageEgressProviderOptions options = new()
            {
                BucketName = "bucket",
                Endpoint = "http://in-memory-s3",
                SecretAccessKey = "secret",
                AccessKeyId = "userId",
                CopyBufferSize = 1024 * 1024
            };

            return options;
        }

        private static MemoryStream ConstructStream(int totalBytes)
        {
            byte[] bytes = new byte[totalBytes];
            Random.Shared.NextBytes(bytes);
            MemoryStream stream = new(bytes);
            return stream;
        }
    }
}
