// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Options
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class AuthenticationOptionsTests
    {
        [Fact]
        public void AuthenticationOptions_Supports_NoConfiguration()
        {
            // Arrange
            AuthenticationOptions options = new();
            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void AuthenticationOptions_Supports_OnlyAzureAd()
        {
            // Arrange
            AuthenticationOptions options = new()
            {
                AzureAd = new AzureAdOptions()
            };
            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void AuthenticationOptions_Supports_OnlyApiKey()
        {
            // Arrange 
            AuthenticationOptions options = new()
            {
                MonitorApiKey = new MonitorApiKeyOptions()
            };
            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void AuthenticationOptions_DoesNotSupport_MultipleModes()
        {
            // Arrange
            AuthenticationOptions options = new()
            {
                MonitorApiKey = new MonitorApiKeyOptions(),
                AzureAd = new AzureAdOptions()
            };
            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
        }
    }
}
