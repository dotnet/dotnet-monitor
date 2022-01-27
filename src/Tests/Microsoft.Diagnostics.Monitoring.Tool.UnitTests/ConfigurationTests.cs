// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{

    public sealed class ConfigurationTests
    {
        private static readonly Dictionary<string, string> AppSettingsContent = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.AppSettings) }
        };

        private static readonly Dictionary<string, string> AspnetEnvironmentVariables = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.AspnetEnvironment) }
        };

        private static readonly Dictionary<string, string> DotnetEnvironmentVariables = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.DotnetEnvironment) }
        };

        private static readonly Dictionary<string, string> MonitorEnvironmentVariables = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.MonitorEnvironment) }
        };

        private static readonly string[] MonitorUrls = new[] { nameof(ConfigurationLevel.HostBuilderSettingsUrl) };

        private static readonly Dictionary<string, string> SharedSettingsContent = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.SharedSettings) }
        };

        private static readonly Dictionary<string, string> UserSettingsContent = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.UserSettings) }
        };

        private readonly ITestOutputHelper _outputHelper;

        public ConfigurationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that when specifying a configuration value at the given configuration level
        /// that the value overrides any other value provided by another configuration source
        /// with lower precedence.
        /// </summary>
        [Theory]
        [InlineData(ConfigurationLevel.None)]
        [InlineData(ConfigurationLevel.HostBuilderSettingsUrl)]
        [InlineData(ConfigurationLevel.DotnetEnvironment)]
        [InlineData(ConfigurationLevel.AspnetEnvironment)]
        [InlineData(ConfigurationLevel.AppSettings)]
        [InlineData(ConfigurationLevel.UserSettings)]
        [InlineData(ConfigurationLevel.SharedSettings)]
        [InlineData(ConfigurationLevel.SharedKeyPerFile)]
        [InlineData(ConfigurationLevel.MonitorEnvironment)]
        public void ConfigurationOrderingTest(ConfigurationLevel level)
        {
            using TemporaryDirectory contentRootDirectory = new(_outputHelper);
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                Authentication = HostBuilderHelper.CreateAuthConfiguration(noAuth: false, tempApiKey: false),
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };
            if (level >= ConfigurationLevel.HostBuilderSettingsUrl)
            {
                settings.Urls = MonitorUrls;
            }

            // Write all of the test files
            if (level >= ConfigurationLevel.AppSettings)
            {
                // This is the appsettings.json file that is normally next to the entrypoint assembly.
                // The location of the appsettings.json is determined by the content root in configuration.
                string appSettingsContent = JsonSerializer.Serialize(AppSettingsContent);
                File.WriteAllText(Path.Combine(contentRootDirectory.FullName, "appsettings.json"), appSettingsContent);
            }
            if (level >= ConfigurationLevel.UserSettings)
            {
                // This is the settings.json file in the user profile directory.
                string userSettingsContent = JsonSerializer.Serialize(UserSettingsContent);
                File.WriteAllText(Path.Combine(userConfigDir.FullName, "settings.json"), userSettingsContent);
            }
            if (level >= ConfigurationLevel.SharedSettings)
            {
                // This is the settings.json file in the shared configuration directory that is visible
                // to all users on the machine e.g. /etc/dotnet-monitor on Unix systems.
                string sharedSettingsContent = JsonSerializer.Serialize(SharedSettingsContent);
                File.WriteAllText(Path.Combine(sharedConfigDir.FullName, "settings.json"), sharedSettingsContent);
            }
            if (level >= ConfigurationLevel.SharedKeyPerFile)
            {
                // This is a key-per-file file in the shared configuration directory. This configuration
                // is typically used when mounting secrets from a Docker volume.
                File.WriteAllText(Path.Combine(sharedConfigDir.FullName, WebHostDefaults.ServerUrlsKey), nameof(ConfigurationLevel.SharedKeyPerFile));
            }

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment(level >= ConfigurationLevel.AspnetEnvironment ? AspnetEnvironmentVariables : null);
            builder.ReplaceDotnetEnvironment(level >= ConfigurationLevel.DotnetEnvironment ? DotnetEnvironmentVariables : null);
            builder.ReplaceMonitorEnvironment(level >= ConfigurationLevel.MonitorEnvironment ? MonitorEnvironmentVariables : null);

            // Build the host and get the Urls property from configuration.
            IHost host = builder.Build();
            IConfiguration rootConfiguration = host.Services.GetRequiredService<IConfiguration>();
            string configuredUrls = rootConfiguration[WebHostDefaults.ServerUrlsKey];

            // Test that the value of the Urls property is the same as it was set
            // for the level of configuration of the test.
            if (level == ConfigurationLevel.None)
            {
                Assert.Null(configuredUrls);
            }
            else
            {
                Assert.Equal(Enum.GetName(level), configuredUrls);
            }
        }

        /// This is the order of configuration sources where a name with a lower
        /// enum value has a lower precedence in configuration.
        public enum ConfigurationLevel
        {
            None,
            HostBuilderSettingsUrl,
            DotnetEnvironment,
            AspnetEnvironment,
            AppSettings,
            UserSettings,
            SharedSettings,
            SharedKeyPerFile,
            MonitorEnvironment
        }
    }
}
