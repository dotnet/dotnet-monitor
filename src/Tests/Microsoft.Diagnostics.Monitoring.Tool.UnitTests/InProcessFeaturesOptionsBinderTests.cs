// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class InProcessFeaturesOptionsBinderTests
    {
        private const string FeatureConfigurationKey = "InProcessFeatures:TestFeature";
        private const string FeatureEnabledConfigurationKey = FeatureConfigurationKey + ":Enabled";

        /// <summary>
        /// Tests that a feature is not enabled if no enablement has been specified.
        /// </summary>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_NoEnablement()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .Build();

            TestFeatureOptions options = new();

            Assert.False(options.Enabled.HasValue);

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: false);

            Assert.True(options.Enabled.HasValue);
            Assert.False(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is not enabled if InProcessFeatures:Enabled is true
        /// but the default enablement of the feature is false.
        /// </summary>
        /// <remarks>
        /// This scenario is for features that are available but require opt-in at the feature level.
        /// </remarks>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_NoDefaultEnablement()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { ConfigurationKeys.InProcessFeatures_Enabled, "true" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: false);

            Assert.True(options.Enabled.HasValue);
            Assert.False(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is enabled if InProcessFeatures:Enabled is true
        /// and the default enablement of the feature is true.
        /// </summary>
        /// <remarks>
        /// This scenario is for features that are automatically enabled when broadly enabling in-process features.
        /// </remarks>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_DefaultEnablement()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { ConfigurationKeys.InProcessFeatures_Enabled, "true" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: true);

            Assert.True(options.Enabled.HasValue);
            Assert.True(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is disabled if InProcessFeatures:Enabled is false
        /// and the default enablement of the feature is false.
        /// </summary>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_DisabledWithDefaultDisabled()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { ConfigurationKeys.InProcessFeatures_Enabled, "false" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: false);

            Assert.True(options.Enabled.HasValue);
            Assert.False(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is disabled if InProcessFeatures:Enabled is false
        /// and the default enablement of the feature is true.
        /// </summary>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_DisabledWithDefaultEnablement()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { ConfigurationKeys.InProcessFeatures_Enabled, "false" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: true);

            Assert.True(options.Enabled.HasValue);
            Assert.False(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is enabled if <![CDATA[InProcessFeatures:<Feature>:Enabled]]> is true.
        /// </summary>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_EnabledProperty()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { FeatureEnabledConfigurationKey, "true" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: true);

            Assert.True(options.Enabled.HasValue);
            Assert.True(options.Enabled.Value);
        }

        /// <summary>
        /// Tests that a feature is disabled if <![CDATA[InProcessFeatures:<Feature>:Enabled]]> is true
        /// and InProcessFeatures:Enabled is false.
        /// </summary>
        [Fact]
        public void InProcessFeaturesOptionsBinder_BindEnabled_EnabledPropertyDisabledFeatures()
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { ConfigurationKeys.InProcessFeatures_Enabled, "false" },
                    { FeatureEnabledConfigurationKey, "true" }
                })
                .Build();

            TestFeatureOptions options = new();

            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                FeatureConfigurationKey,
                configurationRoot,
                enabledByDefault: true);

            Assert.True(options.Enabled.HasValue);
            Assert.False(options.Enabled.Value);
        }

        private sealed class TestFeatureOptions : IInProcessFeatureOptions
        {
            public bool? Enabled { get; set; }
        }
    }
}
