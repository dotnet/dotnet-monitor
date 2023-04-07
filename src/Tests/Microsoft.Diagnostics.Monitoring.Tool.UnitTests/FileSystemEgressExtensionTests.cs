// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class FileSystemEgressExtensionTests
    {
        const string ProviderName = "TestProvider";

        private static readonly string CopyBufferSize_RangeErrorMessage = CreateRangeMessage<int>(
            nameof(FileSystemEgressProviderOptions.CopyBufferSize),
            FileSystemEgressProviderOptions.CopyBufferSize_MinValue.ToString(),
            FileSystemEgressProviderOptions.CopyBufferSize_MaxValue.ToString());

        private readonly ITestOutputHelper _outputHelper;

        public FileSystemEgressExtensionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task FileSystemEgressExtension_EmptyConfiguration_ThrowsException()
        {
            // Arrange
            Mock<IEgressConfigurationProvider> mockConfigurationProvider = new();
            mockConfigurationProvider.Setup(provider => provider.GetProviderConfigurationSection(EgressProviderTypes.FileSystem, ProviderName))
                .Returns(CreateConfigurationSection());

            Mock<ILogger<FileSystemEgressExtension>> mockLogger = new();

            FileSystemEgressExtension extension = new(mockConfigurationProvider.Object, mockLogger.Object);

            // Act & Assert
            OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => extension.EgressArtifact(ProviderName, null, (stream, token) => Task.CompletedTask, CancellationToken.None));
            string errorMessage = Assert.Single(exception.Failures);
            Assert.Equal(CreateRequiredMessage(nameof(FileSystemEgressProviderOptions.DirectoryPath)), errorMessage);
        }

        [Fact]
        public async Task FileSystemEgressExtension_CopyBufferSizeZero_ThrowsException()
        {
            // Arrange
            using TemporaryDirectory temporaryDirectory = new(_outputHelper);

            IConfigurationSection egressProviderSection = CreateConfigurationSection();
            egressProviderSection[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = temporaryDirectory.FullName;
            egressProviderSection[nameof(FileSystemEgressProviderOptions.CopyBufferSize)] = "0";

            Mock<IEgressConfigurationProvider> mockConfigurationProvider = new();
            mockConfigurationProvider.Setup(provider => provider.GetProviderConfigurationSection(EgressProviderTypes.FileSystem, ProviderName))
                .Returns(egressProviderSection);

            Mock<ILogger<FileSystemEgressExtension>> mockLogger = new();

            FileSystemEgressExtension extension = new(mockConfigurationProvider.Object, mockLogger.Object);

            // Act & Assert
            OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => extension.EgressArtifact(ProviderName, null, (stream, token) => Task.CompletedTask, CancellationToken.None));
            string errorMessage = Assert.Single(exception.Failures);
            Assert.Equal(CopyBufferSize_RangeErrorMessage, errorMessage);
        }

        [Fact]
        public async Task FileSystemEgressExtension_DirectoryPath_Success()
        {
            const string ExpectedFileName = "EgressedData.txt";

            // Arrange
            using TemporaryDirectory temporaryDirectory = new(_outputHelper);

            IConfigurationSection egressProviderSection = CreateConfigurationSection();
            egressProviderSection[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = temporaryDirectory.FullName;

            Mock<IEgressConfigurationProvider> mockConfigurationProvider = new();
            mockConfigurationProvider.Setup(provider => provider.GetProviderConfigurationSection(EgressProviderTypes.FileSystem, ProviderName))
                .Returns(egressProviderSection);

            Mock<ILogger<FileSystemEgressExtension>> mockLogger = new();

            FileSystemEgressExtension extension = new(mockConfigurationProvider.Object, mockLogger.Object);

            EgressArtifactSettings settings = new EgressArtifactSettings() { Name = ExpectedFileName };

            // Act
            EgressArtifactResult result = await extension.EgressArtifact(ProviderName, settings, (stream, token) => Task.CompletedTask, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded, "Expected egress to succeed.");
            Assert.True(File.Exists(Path.Combine(temporaryDirectory.FullName, ExpectedFileName)), "Expected file to be written.");
        }

        private static IConfigurationSection CreateConfigurationSection()
        {
            List<IConfigurationProvider> configProviders = new()
            {
                new EmptyConfigurationProvider()
            };
            ConfigurationRoot configurationRoot = new(configProviders);
            string configurationPath = ConfigurationPath.Combine(ConfigurationKeys.Egress, EgressProviderTypes.FileSystem, ProviderName);
            return new ConfigurationSection(configurationRoot, configurationPath);
        }

        private static string CreateRangeMessage<T>(string fieldName, string min, string max)
        {
            return (new RangeAttribute(typeof(T), min, max)).FormatErrorMessage(fieldName);
        }

        private static string CreateRequiredMessage(string fieldName)
        {
            return (new RequiredAttribute()).FormatErrorMessage(fieldName);
        }

        private sealed class EmptyConfigurationProvider : ConfigurationProvider { }
    }
}
