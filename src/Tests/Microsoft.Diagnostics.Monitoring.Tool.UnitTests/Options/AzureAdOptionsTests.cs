// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Options
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class AzureAdOptionsTests
    {
        private static AzureAdOptions GetDefaultOptions()
        {
            return new AzureAdOptions
            {
                TenantId = Guid.NewGuid().ToString("D"),
                ClientId = Guid.NewGuid().ToString("D"),
                RequiredRole = Guid.NewGuid().ToString("D")
            };
        }

        [Fact]
        public void AzureAdOptions_Requires_Role()
        {
            // Arrange
            AzureAdOptions options = GetDefaultOptions();

            options.RequiredRole = null;

            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
        }

        [Fact]
        public void AzureAdOptions_Requires_TenantId()
        {
            // Arrange
            AzureAdOptions options = GetDefaultOptions();

            options.TenantId = null;

            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
        }

        [Fact]
        public void AzureAdOptions_Requires_ClientId()
        {
            // Arrange
            AzureAdOptions options = GetDefaultOptions();

            options.ClientId = null;

            List<ValidationResult> results = new();

            // Act
            bool isValid = Validator.TryValidateObject(options, new(options), results, validateAllProperties: true);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
        }
    }
}
