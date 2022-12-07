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
        public const string SampleArtifactPath = "my/sample/path";
        public const string SampleFailureMessage = "the extension failed";
        private const string TestProviderName = "TestingProvider";
        private const string TestAppName = "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp";
        private const string TestAppExe = TestAppName + ".exe";
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

            const string extensionDisplayName = "InvalidProviderName";

            Assert.Throws<ExtensionException>(() => extensionDiscoverer.FindExtension<IEgressExtension>(extensionDisplayName));
        }

        [Theory]
        [InlineData(ConfigDirectory.ExecutingAssemblyDirectory, null, null)]
        [InlineData(ConfigDirectory.UserConfigDirectory, null, null)]
        [InlineData(ConfigDirectory.SharedConfigDirectory, null, null)]
        [InlineData(ConfigDirectory.DotnetToolsDirectory, DotnetToolsExeDir, TestAppExe)]
        public void FoundExtensionFile_Success(ConfigDirectory configDirectory, string exePath, string exeName)
        {
            IEgressExtension extension = FindEgressExtension(configDirectory, exePath, exeName);

            Assert.NotNull(extension);
        }

        [Fact]
        public async Task ExtensionResponse_Success()
        {
            var extension = FindEgressExtension(ConfigDirectory.UserConfigDirectory, null, null);

            ExtensionEgressPayload payload = new();

            payload.Configuration = new Dictionary<string, string>();
            payload.Configuration.Add("ShouldSucceed", "true");

            CancellationTokenSource tokenSource = new(DefaultTimeout);

            EgressArtifactResult result = await extension.EgressArtifact(payload, GetStream, tokenSource.Token);

            Assert.True(result.Succeeded);
            Assert.Equal(SampleArtifactPath, result.ArtifactPath);
        }

        [Fact]
        public async Task ExtensionResponse_Failure()
        {
            var extension = FindEgressExtension(ConfigDirectory.UserConfigDirectory, null, null);

            ExtensionEgressPayload payload = new();

            payload.Configuration = new Dictionary<string, string>();
            payload.Configuration.Add("ShouldSucceed", "false");

            CancellationTokenSource tokenSource = new(DefaultTimeout);

            EgressArtifactResult result = await extension.EgressArtifact(payload, GetStream, tokenSource.Token);

            Assert.False(result.Succeeded);
            Assert.Equal(SampleFailureMessage, result.FailureMessage);
        }

        private IEgressExtension FindEgressExtension(ConfigDirectory configDirectory, string exePath, string exeName)
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            string directoryName = GetExtensionDirectoryName(settings, configDirectory);

            string destinationPath = configDirectory != ConfigDirectory.DotnetToolsDirectory ? Path.Combine(directoryName, ExtensionsFolder, TestAppName) : Path.Combine(directoryName, DotnetToolsExtensionDir);

            CopyExtensionFiles(destinationPath, exePath, exeName);

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            return extensionDiscoverer.FindExtension<IEgressExtension>(TestProviderName);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            // The test extension currently does not do anything with this stream.
            await stream.WriteAsync(ByteArray);
        }

        private static void CopyExtensionFiles(string destinationPath, string exePath = null, string exeName = null)
        {
            Directory.CreateDirectory(destinationPath);

            bool separateExe = HasSeparateExe(exePath, exeName);
            if (separateExe)
            {
                Directory.CreateDirectory(exePath);
            }

            string testAppPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), TestAppName);

            string sourcePath = Path.GetDirectoryName(testAppPath);

            foreach (string sourceFilePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string replacementPath = separateExe && Path.GetFileName(sourceFilePath) == exeName ? exePath : destinationPath;

                string destinationFilePath = sourceFilePath.Replace(sourcePath, replacementPath);

                string destinationDirPath = Path.GetDirectoryName(destinationFilePath);

                Directory.CreateDirectory(destinationDirPath);

                File.Copy(sourceFilePath, destinationFilePath, true);
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

        private static bool HasSeparateExe(string directory, string exe)
        {
            return !string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(exe);
        }

        private string GetExtensionDirectoryName(HostBuilderSettings settings, ConfigDirectory configDirectory)
        {
            switch (configDirectory)
            {
                case ConfigDirectory.UserConfigDirectory:
                    return settings.UserConfigDirectory;
                case ConfigDirectory.SharedConfigDirectory:
                    return settings.SharedConfigDirectory;
                case ConfigDirectory.DotnetToolsDirectory:
                    return settings.DotnetToolsExtensionDirectory;
                case ConfigDirectory.ExecutingAssemblyDirectory:
                    return settings.ExecutingAssemblyDirectory;
                default:
                    throw new Exception("Config Directory not found.");
            }
        }

        public enum ConfigDirectory
        {
            ExecutingAssemblyDirectory,
            SharedConfigDirectory,
            UserConfigDirectory,
            DotnetToolsDirectory
        }
    }
}
