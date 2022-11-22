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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class EgressExtensibilityTests
    {
        private ITestOutputHelper _outputHelper;
        private const string ExtensionsFolder = "extensions";
        public const string SampleArtifactPath = "my/sample/path";
        public const string SampleFailureMessage = "the extension failed";
        private const string TestAppName = "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp";
        private const string TestAppExe = TestAppName + ".exe";
        private const string DotnetToolsExtensionDir = ".store\\tool-name\\7.0\\tool-name\\7.0\\tools\\net7.0\\any";
        private const string DotnetToolsExeDir = "";

        //private const string ExtensionDefinitionFile = "extension.json";
        //private const string EgressExtensionsDirectory = "EgressExtensionResources";

        public EgressExtensibilityTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /* Test Coverage
         * 
         * Successfully finding extension in each location
         * Proper resolution if conflict in terms of priority
         * Fails properly if extension is not found
         * Extension.json file is properly found and parsed to find extension and DisplayName
         * Fails properly if Extension.json is not found / doesn't contain correct contents
         * Logs from extension are properly passed through to dotnet monitor
         * 
         */

        [Fact]
        public void FoundExtension_Failure()
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            const string extensionDisplayName = "AzureBlobStorage";

            Assert.Throws<ExtensionException>(() => extensionDiscoverer.FindExtension<IEgressExtension>(extensionDisplayName));
        }

        [Theory]
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

            TimeSpan timeout = new(0, 0, 30); // do something real
            CancellationTokenSource tokenSource = new(timeout);

            EgressArtifactResult result = await extension.EgressArtifact(payload, GetStream, tokenSource.Token);

            Assert.True(result.Succeeded);
            Assert.Equal(SampleArtifactPath, result.ArtifactPath);
        }

        private IEgressExtension FindEgressExtension(ConfigDirectory configDirectory, string exePath, string exeName)
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            string directoryName = GetExtensionDirectoryName(settings, configDirectory);

            string destinationPath = configDirectory != ConfigDirectory.DotnetToolsDirectory ? Path.Combine(directoryName, ExtensionsFolder, TestAppName) : Path.Combine(directoryName, DotnetToolsExtensionDir);

            CopyExtensionFiles(directoryName, destinationPath, exePath, exeName);

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            return extensionDiscoverer.FindExtension<IEgressExtension>(TestAppName);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            byte[] byteArray = Enumerable.Repeat((byte)0xDE, 2000).ToArray();

            MemoryStream tempStream = new MemoryStream(byteArray);

            await tempStream.CopyToAsync(stream);
        }

        private static void CopyExtensionFiles(string directoryName, string destinationPath, string exePath = null, string exeName = null)
        {
            Directory.CreateDirectory(destinationPath);

            bool separateExe = HasSeparateExe(exePath, exeName);
            if (separateExe)
            {
                Directory.CreateDirectory(exePath);
            }

            string testAppPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), TestAppName, TargetFrameworkMoniker.Net60); // set this?

            string sourcePath = Path.GetDirectoryName(testAppPath);

            foreach (string sourceFilePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(sourceFilePath);

                string replacementPath = separateExe && fileName == exeName ? exePath : destinationPath;

                string destinationFilePath = sourceFilePath.Replace(sourcePath, replacementPath);

                string destinationDirPath = Path.GetDirectoryName(destinationFilePath);

                Directory.CreateDirectory(destinationDirPath);

                File.Copy(sourceFilePath, destinationFilePath, true);
            }
        }

        private HostBuilderSettings CreateHostBuilderSettings()
        {
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);
            using TemporaryDirectory dotnetToolsConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            return new()
            {
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
                case ConfigDirectory.NextToMeDirectory:
                    //directoryName = .FullName;
                    // NOT HANDLING THIS YET
                    break;
                default:
                    throw new Exception("Config Directory not found.");
            }

            return null;
        }

        /// This is the order of configuration sources where a name with a lower
        /// enum value has a lower precedence in configuration.
        public enum ConfigDirectory
        {
            NextToMeDirectory,
            SharedConfigDirectory,
            UserConfigDirectory,
            DotnetToolsDirectory
        }
    }
}
