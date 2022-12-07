// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class EgressExtensibilityTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

        private ITestOutputHelper _outputHelper;

        private const string ExtensionsFolder = "extensions";
        public const string SampleArtifactPath = "sample\\path";
        public const string SampleFailureMessage = "the extension failed";
        private const string ProviderName = "TestingProvider"; // Must match the name in extension.json
        private const string AppName = "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp";
        private const string AppExe = AppName + ".exe";
        private const string DotnetToolsExtensionDir = ".store\\tool-name\\7.0\\tool-name\\7.0\\tools\\net7.0\\any";
        private const string DotnetToolsExeDir = "";
        private readonly static byte[] ByteArray = Encoding.ASCII.GetBytes(string.Empty);

        public EgressExtensibilityTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void FoundExtension_Failure()
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            Assert.Throws<ExtensionException>(() => extensionDiscoverer.FindExtension<IEgressExtension>("InvalidProviderName"));
        }

        [Theory]
        [InlineData(ConfigDirectory.ExecutingAssemblyDirectory, null, null)]
        [InlineData(ConfigDirectory.UserConfigDirectory, null, null)]
        [InlineData(ConfigDirectory.SharedConfigDirectory, null, null)]
        [InlineData(ConfigDirectory.DotnetToolsExtensionDirectory, DotnetToolsExeDir, AppExe)]
        public void FoundExtensionFile_Success(ConfigDirectory configDirectory, string exePath, string exeName)
        {
            IEgressExtension extension = FindEgressExtension(configDirectory, exePath, exeName);

            Assert.NotNull(extension);
        }

        [Fact]
        public async Task ExtensionResponse_Success()
        {
            EgressArtifactResult result = await GetExtensionResponse(true);

            Assert.True(result.Succeeded);
            Assert.Equal(SampleArtifactPath, result.ArtifactPath);
        }

        [Fact]
        public async Task ExtensionResponse_Failure()
        {
            EgressArtifactResult result = await GetExtensionResponse(false);

            Assert.False(result.Succeeded);
            Assert.Equal(SampleFailureMessage, result.FailureMessage);
        }

        private async Task<EgressArtifactResult> GetExtensionResponse(bool shouldSucceed)
        {
            var extension = FindEgressExtension(ConfigDirectory.UserConfigDirectory);

            ExtensionEgressPayload payload = new();
            payload.Configuration = new Dictionary<string, string>
            {
                { "ShouldSucceed", shouldSucceed.ToString() }
            };

            CancellationTokenSource tokenSource = new(DefaultTimeout);

            return await extension.EgressArtifact(payload, GetStream, tokenSource.Token);
        }

        private IEgressExtension FindEgressExtension(ConfigDirectory configDirectory, string exePath = null, string exeName = null)
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            string directoryName = GetExtensionDirectoryName(settings, configDirectory);

            string extensionDirPath = configDirectory != ConfigDirectory.DotnetToolsExtensionDirectory ? Path.Combine(directoryName, ExtensionsFolder, AppName) : Path.Combine(directoryName, DotnetToolsExtensionDir);

            CopyExtensionFiles(extensionDirPath, exePath, exeName);

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            return extensionDiscoverer.FindExtension<IEgressExtension>(ProviderName);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            // The test extension currently does not do anything with this stream.
            await stream.WriteAsync(ByteArray);
        }

        private static void CopyExtensionFiles(string extensionDirPath, string exePath = null, string exeName = null)
        {
            Directory.CreateDirectory(extensionDirPath);

            string testAppDirPath = Path.GetDirectoryName(AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), AppName));

            bool hasSeparateExe = !string.IsNullOrEmpty(exePath) && !string.IsNullOrEmpty(exeName);

            foreach (string testAppFilePath in Directory.GetFiles(testAppDirPath, "*.*", SearchOption.AllDirectories))
            {
                string extensionFilePath = string.Empty;

                if (hasSeparateExe && Path.GetFileName(testAppFilePath) == exeName)
                {
                    Directory.CreateDirectory(exePath);
                    extensionFilePath = testAppFilePath.Replace(testAppDirPath, exePath);
                }
                else
                {
                    extensionFilePath = testAppFilePath.Replace(testAppDirPath, extensionDirPath);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(extensionFilePath));

                File.Copy(testAppFilePath, extensionFilePath, true);
            }
        }

        private HostBuilderSettings CreateHostBuilderSettings()
        {
            using TemporaryDirectory executingAssemblyDir = new(_outputHelper);
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);
            using TemporaryDirectory dotnetToolsConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            return new()
            {
                ExecutingAssemblyDirectory = executingAssemblyDir.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName,
                DotnetToolsExtensionDirectory = dotnetToolsConfigDir.FullName
            };
        }

        private string GetExtensionDirectoryName(HostBuilderSettings settings, ConfigDirectory configDirectory)
        {
            switch (configDirectory)
            {
                case ConfigDirectory.UserConfigDirectory:
                    return settings.UserConfigDirectory;
                case ConfigDirectory.SharedConfigDirectory:
                    return settings.SharedConfigDirectory;
                case ConfigDirectory.DotnetToolsExtensionDirectory:
                    return settings.DotnetToolsExtensionDirectory;
                case ConfigDirectory.ExecutingAssemblyDirectory:
                    return settings.ExecutingAssemblyDirectory;
                default:
                    throw new ArgumentException("configDirectory not found.");
            }
        }

        public enum ConfigDirectory
        {
            ExecutingAssemblyDirectory,
            SharedConfigDirectory,
            UserConfigDirectory,
            DotnetToolsExtensionDirectory
        }
    }
}
