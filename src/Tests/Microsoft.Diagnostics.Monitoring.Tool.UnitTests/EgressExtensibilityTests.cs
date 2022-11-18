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

        // This only verifies that the Extension.json file is found for the user's extension,
        // not that there is a suitable executable file.
        [Theory]
        [InlineData(ConfigDirectory.UserConfigDirectory)]
        [InlineData(ConfigDirectory.SharedConfigDirectory)]
        //[InlineData(ConfigDirectory.DotnetToolsDirectory)]
        //[InlineData(ConfigDirectory.NextToMeDirectory)]
        public void FoundExtension_Success(ConfigDirectory configDirectory)
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            string directoryName = GetExtensionDirectoryName(settings, configDirectory);

            CopyExtensionFiles(directoryName);

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            var extension = extensionDiscoverer.FindExtension<IEgressExtension>(TestAppName);

            Assert.NotNull(extension);
        }

        [Theory]
        [InlineData(ConfigDirectory.UserConfigDirectory)]
        public async Task ExtensionResponse_Success(ConfigDirectory configDirectory)
        {
            HostBuilderSettings settings = CreateHostBuilderSettings();

            string directoryName = GetExtensionDirectoryName(settings, configDirectory);

            CopyExtensionFiles(directoryName);

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            var extension = extensionDiscoverer.FindExtension<IEgressExtension>(TestAppName);

            ExtensionEgressPayload payload = new();

            payload.Configuration = new Dictionary<string, string>();
            payload.Configuration.Add("ShouldSucceed", "true");

            TimeSpan timeout = new(0, 0, 30); // do something real
            CancellationTokenSource tokenSource = new(timeout);

            EgressArtifactResult result = await extension.EgressArtifact(payload, GetStream, tokenSource.Token);

            Assert.True(result.Succeeded);
            Assert.Equal(SampleArtifactPath, result.ArtifactPath);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            byte[] byteArray = Enumerable.Repeat((byte)0xDE, 2000).ToArray();

            MemoryStream tempStream = new MemoryStream(byteArray);

            await tempStream.CopyToAsync(stream);
        }

        private static void CopyExtensionFiles(string directoryName)
        {
            string destPath = Path.Combine(directoryName, ExtensionsFolder, TestAppName);

            Directory.CreateDirectory(destPath);

            string testAppPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), TestAppName, TargetFrameworkMoniker.Net60); // set this?

            string sourcePath = Path.GetDirectoryName(testAppPath);

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = newPath.Replace(sourcePath, destPath);
                string newDirPath = Path.GetDirectoryName(newFilePath);

                Directory.CreateDirectory(newDirPath);

                File.Copy(newPath, newFilePath, true);
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
