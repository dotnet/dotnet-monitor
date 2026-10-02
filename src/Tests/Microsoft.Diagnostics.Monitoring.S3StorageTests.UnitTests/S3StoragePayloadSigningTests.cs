// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Diagnostics.Monitoring.Extension.S3Storage;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.S3StorageTests.UnitTests
{
    public class S3StoragePayloadSigningTests
    {
        private const string UnsignedPayload = "UNSIGNED-PAYLOAD";
        private const string ContentSha256Header = "x-amz-content-sha256";

        private static S3StorageEgressProviderOptions ConstructOptions() => new()
        {
            BucketName = "bucket",
            Endpoint = "https://example.invalid",
            RegionName = "auto",
            AccessKeyId = "accessKeyId",
            SecretAccessKey = "secretAccessKey"
        };

        [Fact]
        public void ItShouldNotModifyChecksumBehaviorWhenPayloadSigningIsEnabled()
        {
            // The SDK resolves these settings from the environment (AWS_REQUEST_CHECKSUM_CALCULATION /
            // AWS_RESPONSE_CHECKSUM_VALIDATION) and the shared config file, so the resolved values are
            // machine-dependent. Assert that the option-off path leaves them at whatever the SDK
            // resolved, rather than asserting a specific value.
            S3StorageEgressProviderOptions options = ConstructOptions();

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);
            AmazonS3Config untouched = new();

            Assert.Equal(untouched.RequestChecksumCalculation, configuration.RequestChecksumCalculation);
            Assert.Equal(untouched.ResponseChecksumValidation, configuration.ResponseChecksumValidation);
        }

        [Fact]
        public void ItShouldOptOutOfChecksumsWhenPayloadSigningIsDisabled()
        {
            // Checksums travel as an aws-chunked trailer, so leaving them enabled would still emit a
            // STREAMING-...-TRAILER request against an endpoint that cannot accept one.
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.DisablePayloadSigning = true;

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);

            Assert.Equal(RequestChecksumCalculation.WHEN_REQUIRED, configuration.RequestChecksumCalculation);
            Assert.Equal(ResponseChecksumValidation.WHEN_REQUIRED, configuration.ResponseChecksumValidation);
        }

        [Fact]
        public async Task ItShouldSendUnsignedPayloadOnPutWhenPayloadSigningIsDisabled()
        {
            RecordingHttpHandler handler = new();
            S3Storage storage = CreateStorage(handler, disablePayloadSigning: true);

            await storage.PutAsync(new MemoryStream(new byte[64]), CancellationToken.None);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal(UnsignedPayload, GetContentSha256(request));
        }

        [Fact]
        public async Task ItShouldSignPutPayloadByDefault()
        {
            RecordingHttpHandler handler = new();
            S3Storage storage = CreateStorage(handler, disablePayloadSigning: false);

            await storage.PutAsync(new MemoryStream(new byte[64]), CancellationToken.None);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.NotEqual(UnsignedPayload, GetContentSha256(request));
        }

        [Fact]
        public async Task ItShouldSendUnsignedPayloadOnUploadPartWhenPayloadSigningIsDisabled()
        {
            // The multi-part path is the one every artifact takes in production
            // (S3StorageEgressProvider always initiates a multi-part upload).
            RecordingHttpHandler handler = new();
            S3Storage storage = CreateStorage(handler, disablePayloadSigning: true);

            await storage.UploadPartAsync("uploadId", partNumber: 1, partSize: 64, new MemoryStream(new byte[64]), CancellationToken.None);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal(UnsignedPayload, GetContentSha256(request));
        }

        [Fact]
        public async Task ItShouldSignUploadPartPayloadByDefault()
        {
            RecordingHttpHandler handler = new();
            S3Storage storage = CreateStorage(handler, disablePayloadSigning: false);

            await storage.UploadPartAsync("uploadId", partNumber: 1, partSize: 64, new MemoryStream(new byte[64]), CancellationToken.None);

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.NotEqual(UnsignedPayload, GetContentSha256(request));
        }

        [Fact]
        public void ItShouldAcceptDisabledPayloadSigningOverHttps()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.DisablePayloadSigning = true;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }

        [Fact]
        public void ItShouldAcceptDisabledPayloadSigningWithoutAnEndpoint()
        {
            // An empty endpoint usually means the AWS-hosted (HTTPS) endpoints. The SDK can still
            // resolve a non-HTTPS endpoint from the environment; that case is rejected by the SDK's
            // signer at upload time rather than here.
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.Endpoint = null;
            options.DisablePayloadSigning = true;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }

        [Fact]
        public void ItShouldAcceptDisabledPayloadSigningWithSurroundingWhitespace()
        {
            // Environment-injected values can carry stray whitespace; Uri parsing tolerates it and so
            // does the SDK, so validation must not reject it.
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.Endpoint = " https://example.invalid";
            options.DisablePayloadSigning = true;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData("http://example.invalid")]
        [InlineData("HTTP://example.invalid")]
        [InlineData("example.invalid")]
        public void ItShouldNotAcceptDisabledPayloadSigningOverNonHttpsEndpoints(string endpoint)
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.Endpoint = endpoint;
            options.DisablePayloadSigning = true;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage == Strings.ErrorMessage_EgressS3FailedInsecurePayloadSigningOptOut);
        }

        [Fact]
        public void ItShouldAcceptPlainHttpWhenPayloadSigningIsEnabled()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.Endpoint = "http://example.invalid";

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }

        private static S3Storage CreateStorage(RecordingHttpHandler handler, bool disablePayloadSigning)
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.DisablePayloadSigning = disablePayloadSigning;

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);
            configuration.HttpClientFactory = new StubHttpClientFactory(handler);

            IAmazonS3 client = new AmazonS3Client(S3Storage.CreateCredentials(options), configuration);
            return new S3Storage(client, options.BucketName, "objectId", "application/octet-stream",
                useKmsEncryption: false, kmsEncryptionKey: null, disablePayloadSigning: disablePayloadSigning);
        }

        private static string GetContentSha256(HttpRequestMessage request)
        {
            Assert.True(request.Headers.TryGetValues(ContentSha256Header, out IEnumerable<string> values));
            return values.Single();
        }

        /// <summary>
        /// Records every signed request and answers with a minimal success response, so the assertion
        /// is on the wire form the storage service actually receives.
        /// </summary>
        private sealed class RecordingHttpHandler : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Content != null)
                {
                    // Buffer so the request stays inspectable after the SDK disposes its stream.
                    await request.Content.LoadIntoBufferAsync(cancellationToken);
                }
                Requests.Add(request);

                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(string.Empty)
                };
                response.Headers.TryAddWithoutValidation("ETag", "\"0123456789abcdef0123456789abcdef\"");
                return response;
            }
        }

        private sealed class StubHttpClientFactory : Amazon.Runtime.HttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public StubHttpClientFactory(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            public override HttpClient CreateHttpClient(IClientConfig clientConfig)
                => new(_handler, disposeHandler: false);
        }
    }
}
