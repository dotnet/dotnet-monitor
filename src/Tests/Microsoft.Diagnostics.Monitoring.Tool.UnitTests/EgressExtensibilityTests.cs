﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class EgressExtensibilityTests
    {
        private ITestOutputHelper _outputHelper;

        public EgressExtensibilityTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

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

            Assert.Throws<ExtensionException>(() => extensionDiscoverer.FindExtension<IEgressExtension>("InvalidProviderName"));
        }

        [Theory]
        [InlineData(ConfigDirectory.UserConfigDirectory)]
        [InlineData(ConfigDirectory.SharedConfigDirectory)]
        public void FoundExtensionFile_Success(ConfigDirectory configDirectory)
        {
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };

            IEgressExtension extension = FindEgressExtension(configDirectory, settings);

            Assert.NotNull(extension);
        }

        private IEgressExtension FindEgressExtension(ConfigDirectory configDirectory, HostBuilderSettings settings)
        {
            IHost host = TestHostHelper.CreateHost(_outputHelper, rootOptions => { }, host => { }, settings: settings);

            using TemporaryDirectory dotnetToolsConfigDir = new(_outputHelper);
            var dotnetToolsFileSystem = host.Services.GetService<IDotnetToolsFileSystem>();
            dotnetToolsFileSystem.Path = dotnetToolsConfigDir.FullName;

            string directoryName = GetExtensionDirectoryName(settings, dotnetToolsFileSystem, configDirectory);
            string extensionDirPath = Path.Combine(directoryName, EgressExtensibilityTestsConstants.ExtensionsFolder, EgressExtensibilityTestsConstants.AppName);

            CopyExtensionFiles(extensionDirPath);

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();
            return extensionDiscoverer.FindExtension<IEgressExtension>(EgressExtensibilityTestsConstants.ProviderTypeName);
        }

        private static void CopyExtensionFiles(string extensionDirPath)
        {
            Directory.CreateDirectory(extensionDirPath);

            string testAppDirPath = Path.GetDirectoryName(AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), EgressExtensibilityTestsConstants.AppName));

            foreach (string testAppFilePath in Directory.GetFiles(testAppDirPath, "*.*", SearchOption.AllDirectories))
            {
                string extensionFilePath = testAppFilePath.Replace(testAppDirPath, extensionDirPath);

                Directory.CreateDirectory(Path.GetDirectoryName(extensionFilePath));

                File.Copy(testAppFilePath, extensionFilePath, true);
            }
        }

        private static string GetExtensionDirectoryName(HostBuilderSettings settings, IDotnetToolsFileSystem dotnetToolsFileSystem, ConfigDirectory configDirectory)
        {
            switch (configDirectory)
            {
                case ConfigDirectory.UserConfigDirectory:
                    return settings.UserConfigDirectory;
                case ConfigDirectory.SharedConfigDirectory:
                    return settings.SharedConfigDirectory;
                default:
                    throw new ArgumentException("configDirectory not found.");
            }
        }

        public enum ConfigDirectory
        {
            SharedConfigDirectory,
            UserConfigDirectory,
        }
    }
}
