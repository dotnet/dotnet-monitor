// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Amazon.Runtime;
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

        internal class InMemoryS3ClientFactory : S3StorageEgressProvider.AmazonS3ClientFactory
        {
            public readonly InMemoryS3 S3 = new();
            public override IAmazonS3 Create(AWSCredentials awsCredentials, AmazonS3Config configuration) => S3;            
        }

        private readonly InMemoryS3ClientFactory _clientFactory = new();
        private readonly S3StorageEgressProvider _egressProvider;

        public S3StorageEgressProviderTests(ITestOutputHelper outputHelper)
        {
            // Arrange
            TestOutputLoggerProvider loggerProvider = new(outputHelper);
            _egressProvider = new(loggerProvider.CreateLogger<S3StorageEgressProvider>()) { ClientFactory = _clientFactory };
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadFile(EUploadAction uploadAction)
        {
            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = MultiPartUploadStream.MinimumSize * 3 + 1024;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await _egressProvider.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await _egressProvider.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException(nameof(uploadAction))
            };

            // verify
            Assert.Equal($"BucketName={options.BucketName}, Key={artifactSettings.Name}", resourceId);

            var storage = _clientFactory.S3;
            Assert.Single(storage.Bucket(options.BucketName));
            Assert.True(storage.Bucket(options.BucketName).ContainsKey(artifactSettings.Name));
            var data = storage.Bucket(options.BucketName)[artifactSettings.Name];
            Assert.Equal(totalBytes, data.Size);
            Assert.Equal(stream.ToArray(), data.Bytes());
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadEmptyFile(EUploadAction uploadAction)
        {
            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = 0;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await _egressProvider.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await _egressProvider.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException(nameof(uploadAction))
            };

            // verify
            Assert.Equal($"BucketName={options.BucketName}, Key={artifactSettings.Name}", resourceId);

            var storage = _clientFactory.S3;
            Assert.Single(storage.Bucket(options.BucketName));
            Assert.True(storage.Bucket(options.BucketName).ContainsKey(artifactSettings.Name));
            var data = storage.Bucket(options.BucketName)[artifactSettings.Name];
            Assert.Equal(totalBytes, data.Size);
            Assert.Equal(stream.ToArray(), data.Bytes());
        }

        [Theory]
        [InlineData(EUploadAction.ProvideUploadStream)]
        [InlineData(EUploadAction.WriteToProviderStream)]
        public async Task ItShouldUploadFileAndGeneratePreSignedUrl(EUploadAction uploadAction)
        {
            // prepare
            S3StorageEgressProviderOptions options = ConstructEgressProviderSettings();
            options.GeneratePresSignedUrl = true;
            options.PreSignedUrlExpiry = TimeSpan.FromMinutes(10);
            EgressArtifactSettings artifactSettings = ConstructArtifactSettings();

            // perform
            var totalBytes = MultiPartUploadStream.MinimumSize * 3 + 1024;
            using var stream = ConstructStream(totalBytes);
            string resourceId = uploadAction switch
            {
                EUploadAction.ProvideUploadStream => await _egressProvider.EgressAsync(options, _ => Task.FromResult((Stream)stream), artifactSettings, CancellationToken.None),
                EUploadAction.WriteToProviderStream => await _egressProvider.EgressAsync(options, stream.CopyToAsync, artifactSettings, CancellationToken.None),
                _ => throw new ArgumentException(nameof(uploadAction))
            };

            // verify
            var expiration = DateTime.UtcNow.Add(options.PreSignedUrlExpiry!.Value);
            var expectation = $"local/{options.BucketName}/{artifactSettings.Name}/{expiration:yyyyMMddHH}";
            Assert.StartsWith(expectation, resourceId);

            var storage = _clientFactory.S3;
            Assert.Single(storage.Bucket(options.BucketName));
            Assert.True(storage.Bucket(options.BucketName).ContainsKey(artifactSettings.Name));
            var data = storage.Bucket(options.BucketName)[artifactSettings.Name];
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
