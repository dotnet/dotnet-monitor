// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Options
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class AuthenticationOptionsTests
    {
        [Fact]
        public void AuthenticationOptions_Supports_NoConfiguration()
        {
            AuthenticationOptions options = new();
            ValidationHelper.ThrowIfValidationErrors(options);
        }

        [Fact]
        public void AuthenticationOptions_Supports_OnlyAzureAd()
        {
            AuthenticationOptions options = new()
            {
                AzureAd = new AzureAdOptions()
            };
            ValidationHelper.ThrowIfValidationErrors(options);
        }

        [Fact]
        public void AuthenticationOptions_Supports_OnlyApiKey()
        {
            AuthenticationOptions options = new()
            {
                MonitorApiKey = new MonitorApiKeyOptions()
            };
            ValidationHelper.ThrowIfValidationErrors(options);
        }

        [Fact]
        public void AuthenticationOptions_DoesNotSupport_MultipleModes()
        {
            AuthenticationOptions options = new()
            {
                MonitorApiKey = new MonitorApiKeyOptions(),
                AzureAd = new AzureAdOptions()
            };
            Assert.Throws<OptionsValidationException>(() => ValidationHelper.ThrowIfValidationErrors(options));
        }
    }
}
