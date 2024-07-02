// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
                UserConfigDirectory = userConfigDir.FullName,
                ContentRootDirectory = AppContext.BaseDirectory
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
                UserConfigDirectory = userConfigDir.FullName,
                ContentRootDirectory = AppContext.BaseDirectory
            };

            IEgressExtension extension = FindEgressExtension(configDirectory, settings);

            Assert.NotNull(extension);
        }

        [Fact(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4983")]
        public async Task ExtensionResponse_Success()
        {
            EgressArtifactResult result = await GetExtensionResponse(true);

            Assert.True(result.Succeeded);
            Assert.Equal(EgressExtensibilityTestsConstants.SampleArtifactPath, result.ArtifactPath);
        }

        [Fact(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4983")]
        public async Task ExtensionResponse_Failure()
        {
            EgressArtifactResult result = await GetExtensionResponse(false);

            Assert.False(result.Succeeded);
            Assert.Equal(EgressExtensibilityTestsConstants.SampleFailureMessage, result.FailureMessage);
        }

        private async Task<EgressArtifactResult> GetExtensionResponse(bool shouldSucceed)
        {
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName,
                ContentRootDirectory = AppContext.BaseDirectory
            };

            EgressExtension extension = (EgressExtension)FindEgressExtension(ConfigDirectory.UserConfigDirectory, settings);

            // This addresses an issue with the extension process not being able to find the required version of dotnet
            // Runtime Directory example: 'C:\\Users\\abc\\dotnet-monitor\\.dotnet\\shared\\Microsoft.NETCore.App\\6.0.11\\
            string dotnetPath = Directory.GetParent(RuntimeEnvironment.GetRuntimeDirectory()).Parent.Parent.Parent.FullName;
            extension.AddEnvironmentVariable("DOTNET_ROOT", dotnetPath);

            ExtensionEgressPayload payload = new();

            payload.Configuration = new Dictionary<string, string>()
            {
                { "ShouldSucceed", shouldSucceed.ToString() },
                { $"Metadata:{EgressExtensibilityTestsConstants.Key}", EgressExtensibilityTestsConstants.Value }
            };

            CancellationTokenSource tokenSource = new(CommonTestTimeouts.GeneralTimeout);

            return await extension.EgressArtifact(payload, GetStream, ExtensionMode.Execute, tokenSource.Token);
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

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string executablePath = Path.Combine(extensionDirPath, EgressExtensibilityTestsConstants.AppName);

#if NET7_0_OR_GREATER
                File.SetUnixFileMode(executablePath, UnixFileMode.UserExecute);
#else
                ProcessStartInfo startInfo = new()
                {
                    FileName = "chmod",
                    UseShellExecute = true
                };
                startInfo.ArgumentList.Add("+x");
                startInfo.ArgumentList.Add(executablePath);

                using Process proc = Process.Start(startInfo);
                if (!proc.WaitForExit(60_000)) // 1 minute
                {
                    throw new InvalidOperationException("Unable to make extension executable: Timed out.");
                }
                if (0 != proc.ExitCode)
                {
                    throw new InvalidOperationException("Unable to make extension executable: Failed.");
                }
#endif
            }

            var extensionDiscoverer = host.Services.GetService<ExtensionDiscoverer>();
            return extensionDiscoverer.FindExtension<IEgressExtension>(EgressExtensibilityTestsConstants.ProviderName);
        }

        private static async Task GetStream(Stream stream, CancellationToken cancellationToken)
        {
            // The test extension currently does not do anything with this stream.
            await stream.WriteAsync(EgressExtensibilityTestsConstants.ByteArray, cancellationToken);
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

        private static bool IsExecutablePath(string path)
        {
            if (Path.GetFileNameWithoutExtension(path) == EgressExtensibilityTestsConstants.AppName)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Path.GetExtension(path) == ".exe")
                {
                    return true;
                }
                else
                {
                    return Path.GetExtension(path) == string.Empty;
                }
            }

            return false;
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

        private static bool IsNotAlpineAndNotArm64 => TestConditions.IsNotAlpine && TestConditions.IsNotArm64;

        public enum ConfigDirectory
        {
            SharedConfigDirectory,
            UserConfigDirectory,
        }
    }
}
