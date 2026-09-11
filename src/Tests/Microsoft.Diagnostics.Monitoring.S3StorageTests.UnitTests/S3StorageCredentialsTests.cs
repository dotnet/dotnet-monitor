// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Diagnostics.Monitoring.Extension.S3Storage;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.S3StorageTests.UnitTests
{
    public class S3StorageCredentialsTests
    {
        private const string AccessKeyId = "accessKeyId";
        private const string SecretAccessKey = "secretAccessKey";
        private const string SessionToken = "sessionToken";

        private static S3StorageEgressProviderOptions ConstructOptions() => new()
        {
            BucketName = "bucket",
            Endpoint = "https://example.invalid",
            RegionName = "auto"
        };

        [Fact]
        public async Task ItShouldUseSessionCredentialsWhenSessionTokenIsSpecified()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;
            options.SessionToken = SessionToken;

            AWSCredentials credentials = S3Storage.CreateCredentials(options);

            Assert.IsType<SessionAWSCredentials>(credentials);

            ImmutableCredentials immutableCredentials = await credentials.GetCredentialsAsync();
            Assert.Equal(AccessKeyId, immutableCredentials.AccessKey);
            Assert.Equal(SecretAccessKey, immutableCredentials.SecretKey);
            Assert.Equal(SessionToken, immutableCredentials.Token);
            Assert.True(immutableCredentials.UseToken);
        }

        [Fact]
        public async Task ItShouldUseBasicCredentialsWhenSessionTokenIsNotSpecified()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;

            AWSCredentials credentials = S3Storage.CreateCredentials(options);

            Assert.IsType<BasicAWSCredentials>(credentials);

            ImmutableCredentials immutableCredentials = await credentials.GetCredentialsAsync();
            Assert.Equal(AccessKeyId, immutableCredentials.AccessKey);
            Assert.Equal(SecretAccessKey, immutableCredentials.SecretKey);
            Assert.False(immutableCredentials.UseToken);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ItShouldUseBasicCredentialsWhenSessionTokenIsEmpty(string sessionToken)
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;
            options.SessionToken = sessionToken;

            AWSCredentials credentials = S3Storage.CreateCredentials(options);

            Assert.IsType<BasicAWSCredentials>(credentials);
        }

        [Fact]
        public void ItShouldApplyEndpointToConfigurationWhenSessionTokenIsSpecified()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;
            options.SessionToken = SessionToken;
            options.ForcePathStyle = true;

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);

            // The AWS SDK normalizes the service URL with a trailing slash.
            Assert.Equal(options.Endpoint + "/", configuration.ServiceURL);
            Assert.Equal(options.RegionName, configuration.AuthenticationRegion);
            Assert.True(configuration.ForcePathStyle);
        }

        [Fact]
        public void ItShouldApplyEndpointToConfigurationWhenUsingAnAwsProfile()
        {
            // Regression test: the endpoint / region / path-style settings used to be applied only on the
            // access key code path, which left the AWS profile code path without any endpoint at all.
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AwsProfileName = "profile";
            options.ForcePathStyle = true;

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);

            // The AWS SDK normalizes the service URL with a trailing slash.
            Assert.Equal(options.Endpoint + "/", configuration.ServiceURL);
            Assert.Equal(options.RegionName, configuration.AuthenticationRegion);
            Assert.True(configuration.ForcePathStyle);
        }

        [Fact]
        public void ItShouldUseRegionEndpointWhenNoEndpointIsSpecified()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.Endpoint = null;
            options.RegionName = "us-east-1";

            AmazonS3Config configuration = S3Storage.CreateConfiguration(options);

            Assert.True(string.IsNullOrEmpty(configuration.ServiceURL));
            Assert.Equal(RegionEndpoint.USEast1, configuration.RegionEndpoint);
            Assert.True(string.IsNullOrEmpty(configuration.AuthenticationRegion));
        }

        [Fact]
        public void ItShouldAcceptSessionTokenWithAccessKeyAndSecret()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;
            options.SessionToken = SessionToken;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(AccessKeyId, null)]
        [InlineData(null, SecretAccessKey)]
        public void ItShouldNotAcceptSessionTokenWithoutAccessKeyAndSecret(string accessKeyId, string secretAccessKey)
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = accessKeyId;
            options.SecretAccessKey = secretAccessKey;
            options.SessionToken = SessionToken;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage == Strings.ErrorMessage_EgressS3FailedMissingSessionCredentials);
        }

        [Fact]
        public void ItShouldAcceptNoSessionToken()
        {
            S3StorageEgressProviderOptions options = ConstructOptions();
            options.AccessKeyId = AccessKeyId;
            options.SecretAccessKey = SecretAccessKey;

            List<ValidationResult> results = new();
            bool valid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

            Assert.True(valid);
            Assert.Empty(results);
        }
    }
}
