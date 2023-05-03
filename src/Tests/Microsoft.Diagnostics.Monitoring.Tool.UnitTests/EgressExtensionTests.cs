// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class EgressExtensionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EgressExtension_Execute_SuccessResponse(bool useExecutable)
        {
            EgressArtifactResult result = await GetExtensionResponse(useExecutable, shouldSucceed: true);

            Assert.True(result.Succeeded);
            Assert.Equal(EgressExtensibilityTestsConstants.SampleArtifactPath, result.ArtifactPath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EgressExtension_Execute_FailureResponse(bool useExecutable)
        {
            EgressArtifactResult result = await GetExtensionResponse(useExecutable, shouldSucceed: false);

            Assert.False(result.Succeeded);
            Assert.Equal(EgressExtensibilityTestsConstants.SampleFailureMessage, result.FailureMessage);
        }

        private static async Task<EgressArtifactResult> GetExtensionResponse(bool useExecutable, bool shouldSucceed)
        {
            EgressExtension extension = CreateExtension(useExecutable, shouldSucceed);

            if (!useExecutable)
            {
                // Instruct the extension apphost where the dotnet root is in order to use repository installation
                extension.AddEnvironmentVariable("DOTNET_ROOT", Path.GetDirectoryName(DotNetHost.ExecutablePath));
            }

            using CancellationTokenSource tokenSource = new(CommonTestTimeouts.GeneralTimeout);

            EgressArtifactSettings settings = new()
            {
                Name = "test.txt",
                ContentType = ContentTypes.TextPlain
            };

            return await extension.EgressArtifact(
                EgressExtensibilityTestsConstants.ProviderName,
                settings,
                GetStream,
                tokenSource.Token);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            // The test extension currently does not do anything with this stream.
            await stream.WriteAsync(EgressExtensibilityTestsConstants.ByteArray, cancellationToken);
        }

        private static EgressExtension CreateExtension(bool useExecutable, bool shouldSucceed)
        {
            // Logger
            Mock<ILogger<EgressExtension>> mockLogger = new();

            // Configuration provider
            List<IConfigurationProvider> configProviders = new()
            {
                new EmptyConfigurationProvider()
            };
            ConfigurationRoot configurationRoot = new(configProviders);
            string configurationPath = ConfigurationPath.Combine(ConfigurationKeys.Egress, EgressExtensibilityTestsConstants.ProviderTypeName, EgressExtensibilityTestsConstants.ProviderName);
            ConfigurationSection configurationSection = new(configurationRoot, configurationPath);

            // Configuration data
            configurationSection["ShouldSucceed"] = shouldSucceed.ToString();
            configurationSection[ConfigurationPath.Combine("Metadata", EgressExtensibilityTestsConstants.Key)] = EgressExtensibilityTestsConstants.Value;

            Mock<IEgressConfigurationProvider> mockConfigurationProvider = new();
            mockConfigurationProvider.Setup(provider => provider.GetProviderConfigurationSection(EgressExtensibilityTestsConstants.ProviderTypeName, EgressExtensibilityTestsConstants.ProviderName))
                .Returns(configurationSection);

            // Manifest
            string assemblyPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), EgressExtensibilityTestsConstants.AppName);
            string extensionPath = Path.GetDirectoryName(assemblyPath);

            ExtensionManifest manifest = ExtensionManifest.FromPath(Path.Combine(extensionPath, ExtensionManifest.DefaultFileName));

            if (useExecutable)
            {
                manifest.AssemblyFileName = null;
                manifest.ExecutableFileName = EgressExtensibilityTestsConstants.AppName;
            }
            else
            {
                manifest.AssemblyFileName = Path.GetFileNameWithoutExtension(assemblyPath);
                manifest.ExecutableFileName = null;
            }

            // Create extension
            return new EgressExtension(
                manifest,
                extensionPath,
                mockConfigurationProvider.Object,
                mockLogger.Object);
        }
    }
}
