// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Options
{
    /*
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class S3StorageEgressProviderOptionsTests
    {
        private S3StorageEgressProviderOptions _options = new()
        {
            Endpoint = "http://localhost:8080",
            BucketName = "bucket"
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ItShouldNotAcceptEmptyBucketName(string bucketName)
        {
            _options.BucketName = bucketName;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.False(valid);
            Assert.Single(result);
            Assert.StartsWith($"The {nameof(S3StorageEgressProviderOptions.BucketName)} field", result[0].ErrorMessage);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "SecretAccessKey")]
        public void ItShouldAcceptEmptySecrets(string accessKeyId, string secretAccessKey)
        {
            _options.AccessKeyId = accessKeyId;
            _options.SecretAccessKey = secretAccessKey;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.True(valid);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ItShouldNotAcceptEmptySecrets(string secretAccessKey)
        {
            _options.AccessKeyId = "accessKey";
            _options.SecretAccessKey = secretAccessKey;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.False(valid);
            Assert.Single(result);
            Assert.Equal(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingSecrets, result[0].ErrorMessage);
        }

        [Fact]
        public void ItShouldAcceptPreSignedUrlExpiryToBeNull()
        {
            _options.PreSignedUrlExpiry = null;
            _options.GeneratePreSignedUrl = false;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.True(valid);
            Assert.Empty(result);
        }

        [Fact]
        public void ItShouldNotAcceptPreSignedUrlExpiryToBeNull()
        {
            _options.PreSignedUrlExpiry = null;
            _options.GeneratePreSignedUrl = true;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.False(valid);
            Assert.Single(result);
            Assert.Equal(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(S3StorageEgressProviderOptions.PreSignedUrlExpiry)), result[0].ErrorMessage);
        }

        [Theory]
        [InlineData("00:00:59")]
        [InlineData("1.00:00:01")]
        public void ItShouldNotAcceptInvalidPreSignedUrlExpiry(string preSignedUrlExpiry)
        {
            _options.PreSignedUrlExpiry = TimeSpan.Parse(preSignedUrlExpiry);
            _options.GeneratePreSignedUrl = true;

            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(_options, new(_options), result, true);

            Assert.False(valid);
            Assert.Single(result);
            Assert.Equal($"The field {nameof(S3StorageEgressProviderOptions.PreSignedUrlExpiry)} must be between 00:01:00 and 1.00:00:00.", result[0].ErrorMessage);
        }
    }
    */
}
