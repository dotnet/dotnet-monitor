// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };

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
        public async Task FoundExtension_Success(ConfigDirectory configDirectory)
        {
            //const string extensionDisplayName = "AzureBlobStorage"; // This has to be the same as the display name within the Extension.json file
            //const string extensionDirectoryName = "dotnet-monitor-egress-azureblobstorage";

            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);
            using TemporaryDirectory dotnetToolsConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName,
                //DotnetToolsExtensionDirectory = dotnetToolsConfigDir
            };

            string directoryName = string.Empty;

            switch (configDirectory)
            {
                case ConfigDirectory.UserConfigDirectory:
                    directoryName = userConfigDir.FullName;
                    break;
                case ConfigDirectory.SharedConfigDirectory:
                    directoryName = sharedConfigDir.FullName;
                    break;
                case ConfigDirectory.DotnetToolsDirectory:
                    directoryName = dotnetToolsConfigDir.FullName;
                    break;
                case ConfigDirectory.NextToMeDirectory:
                    //directoryName = .FullName;
                    // NOT HANDLING THIS YET
                    break;
                default:
                    throw new Exception("Config Directory not found.");
            }

            string appName = "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp";

            string destPath = Path.Combine(directoryName, ExtensionsFolder, appName);

            Directory.CreateDirectory(destPath);

            string testAppPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), appName, TargetFrameworkMoniker.Net60);

            string sourcePath = Path.GetDirectoryName(testAppPath);


            //string sourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), EgressExtensionsDirectory);

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string newFilePath = newPath.Replace(sourcePath, destPath);
                string newDirPath = Path.GetDirectoryName(newFilePath);

                Directory.CreateDirectory(newDirPath);

                File.Copy(newPath, newFilePath, true);
            }

            /*File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), EgressExtensionsDirectory, ExtensionDefinitionFile),
                Path.Combine(destPath));*/


            /*File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), EgressExtensionsDirectory, ExtensionDefinitionFile),
                Path.Combine(destPath, ExtensionDefinitionFile));*/


            /*File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), EgressExtensionsDirectory),
                destPath); // This won't work for special dotnet tools case*/

            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();

            var extension = extensionDiscoverer.FindExtension<IEgressExtension>(appName);

            ExtensionEgressPayload payload = new();

            payload.Configuration = new Dictionary<string, string>();
            payload.Configuration.Add("ShouldSucceed", "true");

            TimeSpan timeout = new(0, 0, 30); // do something real
            CancellationTokenSource tokenSource = new(timeout);

            var result = await extension.EgressArtifact(payload, GetStream, tokenSource.Token);

            _outputHelper.WriteLine("Succeeded? " + result.Succeeded);
        }

        [Fact]
        public static void ExtensionResponse_Success()
        {
            string testAppPath = AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp", TargetFrameworkMoniker.Net60);

        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            byte[] byteArray = Enumerable.Repeat((byte)0xDE, 2000).ToArray();

            MemoryStream tempStream = new MemoryStream(byteArray);

            await tempStream.CopyToAsync(stream);
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
