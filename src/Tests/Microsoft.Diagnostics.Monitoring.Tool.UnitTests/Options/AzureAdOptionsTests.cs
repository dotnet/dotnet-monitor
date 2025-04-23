// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Options
{
    public class AzureAdOptionsTestsFixture : WebApplicationFactory<Program>
    {
    }

    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class AzureAdOptionsTests : IClassFixture<AzureAdOptionsTestsFixture>
    {
        private readonly AzureAdOptionsTestsFixture _fixture;

        public AzureAdOptionsTests(AzureAdOptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

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
            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;

            // Act
            bool isValid = ValidationHelper.TryValidateObject(options, typeof(AzureAdOptions), validationOptions, _fixture.Services, results);

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
            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;

            // Act
            bool isValid = ValidationHelper.TryValidateObject(options, typeof(AzureAdOptions), validationOptions, _fixture.Services, results);

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
            var validationOptions = _fixture.Services.GetRequiredService<IOptions<ValidationOptions>>().Value;

            // Act
            bool isValid = ValidationHelper.TryValidateObject(options, typeof(AzureAdOptions), validationOptions, _fixture.Services, results);

            // Assert
            Assert.False(isValid);
            Assert.Single(results);
        }
    }
}
