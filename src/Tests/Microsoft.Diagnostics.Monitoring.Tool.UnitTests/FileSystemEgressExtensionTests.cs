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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class FileSystemEgressExtensionTests
    {
        const string ProviderName = "TestProvider";
        const string ExpectedFileName = "EgressedData.txt";
        const string ExpectedFileContent = "This is egressed data.";

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
            IEgressExtension extension = CreateExtension();

            // Act & Assert
            OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => extension.EgressArtifact(ProviderName, CreateSettings(), WriteFileContent, CancellationToken.None));
            string errorMessage = Assert.Single(exception.Failures);
            Assert.Equal(CreateRequiredMessage(nameof(FileSystemEgressProviderOptions.DirectoryPath)), errorMessage);
        }

        [Fact]
        public async Task FileSystemEgressExtension_CopyBufferSizeZero_ThrowsException()
        {
            // Arrange
            using TemporaryDirectory targetDirectory = new(_outputHelper);

            IEgressExtension extension = CreateExtension(section =>
            {
                section[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = targetDirectory.FullName;
                section[nameof(FileSystemEgressProviderOptions.CopyBufferSize)] = "0";
            });

            // Act & Assert
            OptionsValidationException exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => extension.EgressArtifact(ProviderName, CreateSettings(), WriteFileContent, CancellationToken.None));
            string errorMessage = Assert.Single(exception.Failures);
            Assert.Equal(CopyBufferSize_RangeErrorMessage, errorMessage);
        }

        [Fact]
        public async Task FileSystemEgressExtension_DirectoryPath_Success()
        {
            // Arrange
            using TemporaryDirectory targetDirectory = new(_outputHelper);

            IEgressExtension extension = CreateExtension(section =>
            {
                section[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = targetDirectory.FullName;
            });

            // Act
            EgressArtifactResult result = await extension.EgressArtifact(
                ProviderName,
                CreateSettings(),
                WriteFileContent,
                CancellationToken.None);

            // Assert
            ValidateSuccess(result, targetDirectory);
        }

        [Fact]
        public async Task FileSystemEgressExtension_CopyBufferSize_Success()
        {
            // Arrange
            using TemporaryDirectory targetDirectory = new(_outputHelper);

            IEgressExtension extension = CreateExtension(section =>
            {
                section[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = targetDirectory.FullName;
                section[nameof(FileSystemEgressProviderOptions.CopyBufferSize)] = "10";
            });

            // Act
            EgressArtifactResult result = await extension.EgressArtifact(
                ProviderName,
                CreateSettings(),
                WriteFileContent,
                CancellationToken.None);

            // Assert
            ValidateSuccess(result, targetDirectory);
        }

        [Fact]
        public async Task FileSystemEgressExtension_IntermediateDirectoryPath_Success()
        {
            // Arrange
            using TemporaryDirectory targetDirectory = new(_outputHelper);
            using TemporaryDirectory intermediateDirectory = new(_outputHelper);

            IEgressExtension extension = CreateExtension(section =>
            {
                section[nameof(FileSystemEgressProviderOptions.DirectoryPath)] = targetDirectory.FullName;
                section[nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath)] = intermediateDirectory.FullName;
            });

            // Act
            EgressArtifactResult result = await extension.EgressArtifact(
                ProviderName,
                CreateSettings(),
                WriteFileContent,
                CancellationToken.None);

            // Assert
            ValidateSuccess(result, targetDirectory);
            DirectoryInfo intermediateDirInfo = new DirectoryInfo(intermediateDirectory.FullName);
            Assert.True(intermediateDirInfo.Exists, "Intermediate directory should still exist.");
            Assert.False(intermediateDirInfo.EnumerateFiles().Any(), "Intermediate directory should not contain any files.");
        }

        private static IEgressExtension CreateExtension(Action<ConfigurationSection> callback = null)
        {
            List<IConfigurationProvider> configProviders = new()
            {
                new EmptyConfigurationProvider()
            };
            ConfigurationRoot configurationRoot = new(configProviders);
            string configurationPath = ConfigurationPath.Combine(ConfigurationKeys.Egress, EgressProviderTypes.FileSystem, ProviderName);
            ConfigurationSection configurationSection = new(configurationRoot, configurationPath);

            callback?.Invoke(configurationSection);

            Mock<IEgressConfigurationProvider> mockConfigurationProvider = new();
            mockConfigurationProvider.Setup(provider => provider.GetProviderConfigurationSection(EgressProviderTypes.FileSystem, ProviderName))
                .Returns(configurationSection);

            Mock<ILogger<FileSystemEgressExtension>> mockLogger = new();
            Mock<IServiceProvider> mockServiceProvider = new();

            return new FileSystemEgressExtension(mockServiceProvider.Object, mockConfigurationProvider.Object, mockLogger.Object);
        }

        private static EgressArtifactSettings CreateSettings()
        {
            return new EgressArtifactSettings()
            {
                Name = ExpectedFileName
            };
        }

        private static string CreateRangeMessage<T>(string fieldName, string min, string max)
        {
            return (new RangeAttribute(typeof(T), min, max)).FormatErrorMessage(fieldName);
        }

        private static string CreateRequiredMessage(string fieldName)
        {
            return (new RequiredAttribute()).FormatErrorMessage(fieldName);
        }

        private static async Task WriteFileContent(Stream stream, CancellationToken token)
        {
            using StreamWriter writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteAsync(ExpectedFileContent.AsMemory(), token);
        }

        private static void ValidateSuccess(EgressArtifactResult result, TemporaryDirectory targetDirectory)
        {
            Assert.True(result.Succeeded, "Expected egress to succeed.");
            string filePath = Path.Combine(targetDirectory.FullName, ExpectedFileName);
            Assert.True(File.Exists(filePath), "Expected file to be written.");
            string content = File.ReadAllText(filePath);
            Assert.Equal(ExpectedFileContent, content);
        }

        private sealed class EmptyConfigurationProvider : ConfigurationProvider { }
    }
}
