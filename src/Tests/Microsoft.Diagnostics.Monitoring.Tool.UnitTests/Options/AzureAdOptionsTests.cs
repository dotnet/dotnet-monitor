// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Options;
using System;
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
                ClientId = Guid.NewGuid().ToString("D"),
                RequiredRole = "Application.Access",
                RequiredScope = "access_as_user"
            };
        }

        [Fact]
        public void AzureAdOptions_Supports_GuidTenantId()
        {
            AzureAdOptions options = GetDefaultOptions();

            options.TenantId = Guid.NewGuid().ToString("D");

            ValidationHelper.ThrowIfValidationErrors(options);
        }

        [Fact]
        public void AzureAdOptions_Supports_WellKnownTenantId()
        {
            AzureAdOptions options = GetDefaultOptions();

            options.TenantId = "common";

            ValidationHelper.ThrowIfValidationErrors(options);
        }

        [Fact]
        public void AzureAdOptions_Requires_RoleOrScope()
        {
            AzureAdOptions options = GetDefaultOptions();

            options.RequiredScope = null;
            options.RequiredRole = null;

            Assert.Throws<OptionsValidationException>(() => ValidationHelper.ThrowIfValidationErrors(options));
        }

        [Fact]
        public void AzureAdOptions_Requires_ClientId()
        {
            AzureAdOptions options = GetDefaultOptions();

            options.ClientId = null;

            Assert.Throws<OptionsValidationException>(() => ValidationHelper.ThrowIfValidationErrors(options));
        }
    }
}
