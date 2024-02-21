// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
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

        private static readonly Dictionary<string, string> UserProvidedFileSettingsContent = new(StringComparer.Ordinal)
        {
            { WebHostDefaults.ServerUrlsKey, nameof(ConfigurationLevel.UserProvidedFileSettings) }
        };

        // This needs to be updated and kept in order for any future configuration sections
        private static readonly List<string> OrderedConfigurationKeys = new()
        {
            "urls",
            "Kestrel",
            "Templates",
            "CollectionRuleDefaults",
            "GlobalCounter",
            "CollectionRules",
            "CorsConfiguration",
            "DiagnosticPort",
            "InProcessFeatures",
            "Metrics",
            "Storage",
            "DefaultProcess",
            "Logging",
            "DotnetMonitorDebug",
            "Authentication",
            "Egress"
        };

        private const string ExpectedShowSourcesConfigurationsDirectory = "ExpectedShowSourcesConfigurations";

        private const string ExpectedConfigurationsDirectory = "ExpectedConfigurations";

        private const string SampleConfigurationsDirectory = "SampleConfigurations";

        private const string UserProvidedSettingsFileName = "UserSpecifiedFile.json"; // Note: if this name is updated, it must also be updated in the expected show sources configuration files

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
        [InlineData(ConfigurationLevel.UserProvidedFileSettings)]
        public void ConfigurationOrderingTest(ConfigurationLevel level)
        {
            using TemporaryDirectory contentRootDirectory = new(_outputHelper);
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);
            using TemporaryDirectory userProvidedConfigDir = new(_outputHelper);

            string userProvidedConfigFullPath = Path.Combine(userProvidedConfigDir.FullName, UserProvidedSettingsFileName);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                AuthenticationMode = StartupAuthenticationMode.Deferred,
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName,
                UserProvidedConfigFilePath = level >= ConfigurationLevel.UserProvidedFileSettings ? new FileInfo(userProvidedConfigFullPath) : null
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
            if (level >= ConfigurationLevel.UserProvidedFileSettings)
            {
                // This is the user-provided file in the directory specified on the command-line
                string userSpecifiedFileSettingsContent = JsonSerializer.Serialize(UserProvidedFileSettingsContent);
                File.WriteAllText(userProvidedConfigFullPath, userSpecifiedFileSettingsContent);
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

        /// <summary>
        /// Instead of having to explicitly define every expected value, this reuses the individual categories to ensure they
        /// assemble properly when combined.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FullConfigurationTest(bool redact)
        {
            using TemporaryDirectory contentRootDirectory = new(_outputHelper);
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                AuthenticationMode = StartupAuthenticationMode.Deferred,
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };

            // This is the settings.json file in the user profile directory.
            File.WriteAllText(Path.Combine(userConfigDir.FullName, "settings.json"), ConstructSettingsJson());

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment();
            builder.ReplaceDotnetEnvironment();
            builder.ReplaceMonitorEnvironment();

            // Build the host and get the configuration.
            IHost host = builder.Build();
            IConfiguration rootConfiguration = host.Services.GetRequiredService<IConfiguration>();

            string generatedConfig = WriteAndRetrieveConfiguration(rootConfiguration, redact);

            Assert.Equal(CleanWhitespace(ConstructExpectedOutput(redact, ExpectedConfigurationsDirectory)), CleanWhitespace(generatedConfig));
        }

        /// <summary>
        /// This is a full configuration test that lists the configuration provider source for each piece of config.
        /// Instead of having to explicitly define every expected value, this reuses the individual categories to ensure they
        /// assemble properly when combined.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FullConfigurationWithSourcesTest(bool redact)
        {
            using TemporaryDirectory contentRootDirectory = new(_outputHelper);
            using TemporaryDirectory sharedConfigDir = new(_outputHelper);
            using TemporaryDirectory userConfigDir = new(_outputHelper);
            using TemporaryDirectory userProvidedConfigDir = new(_outputHelper);

            string userProvidedConfigFullPath = Path.Combine(userProvidedConfigDir.FullName, UserProvidedSettingsFileName);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                AuthenticationMode = StartupAuthenticationMode.Deferred,
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName,
                UserProvidedConfigFilePath = new FileInfo(userProvidedConfigFullPath)
            };

            settings.Urls = new[] { "https://localhost:44444" }; // This corresponds to the value in SampleConfigurations/URLs.json

            // This is the user-provided file in the directory specified on the command-line
            File.WriteAllText(userProvidedConfigFullPath, ConstructSettingsJson("Egress.json"));

            // This is the settings.json file in the user profile directory.
            File.WriteAllText(Path.Combine(userConfigDir.FullName, "settings.json"), ConstructSettingsJson("CollectionRules.json", "CollectionRuleDefaults.json", "Templates.json"));

            // This is the appsettings.json file that is normally next to the entrypoint assembly.
            // The location of the appsettings.json is determined by the content root in configuration.
            File.WriteAllText(Path.Combine(contentRootDirectory.FullName, "appsettings.json"), ConstructSettingsJson("Storage.json", "Authentication.json"));

            // This is the settings.json file in the shared configuration directory that is visible
            // to all users on the machine e.g. /etc/dotnet-monitor on Unix systems.
            File.WriteAllText(Path.Combine(sharedConfigDir.FullName, "settings.json"), ConstructSettingsJson("Logging.json", "Metrics.json", "InProcessFeatures.json", "DotnetMonitorDebug.json"));

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment(ShowSourcesTestsConstants.DefaultProcess_EnvironmentVariables);
            builder.ReplaceDotnetEnvironment(ShowSourcesTestsConstants.DiagnosticPort_EnvironmentVariables);
            builder.ReplaceMonitorEnvironment(ShowSourcesTestsConstants.GlobalCounter_EnvironmentVariables);

            // Build the host and get the configuration.
            IHost host = builder.Build();
            IConfiguration rootConfiguration = host.Services.GetRequiredService<IConfiguration>();

            string generatedConfig = WriteAndRetrieveConfiguration(rootConfiguration, redact, showSources: true);

            Assert.Equal(CleanWhitespace(ConstructExpectedOutput(redact, ExpectedShowSourcesConfigurationsDirectory)), CleanWhitespace(generatedConfig));
        }

        /// <summary>
        /// Tests that the connection mode is set correctly for various configurations of the diagnostic port
        /// </summary>
        [Theory]
        [MemberData(nameof(GetConnectionModeTestArguments))]
        public void ConnectionModeTest(string fileName, IDictionary<string, string> diagnosticPortEnvironmentVariables)
        {
            TemporaryDirectory contentRootDirectory = new(_outputHelper);
            TemporaryDirectory sharedConfigDir = new(_outputHelper);
            TemporaryDirectory userConfigDir = new(_outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                AuthenticationMode = StartupAuthenticationMode.Deferred,
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment();
            builder.ReplaceDotnetEnvironment();
            builder.ReplaceMonitorEnvironment(diagnosticPortEnvironmentVariables);

            // Build the host and get the configuration
            IHost host = builder.Build();
            IConfiguration rootConfiguration = host.Services.GetRequiredService<IConfiguration>();

            string generatedConfig = WriteAndRetrieveConfiguration(rootConfiguration, redact: false);

            string expectedDiagnosticPortConfig = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DiagnosticPortConfigurations", fileName));

            Assert.Contains(CleanWhitespace(expectedDiagnosticPortConfig), CleanWhitespace(generatedConfig));
        }

        private string WriteAndRetrieveConfiguration(IConfiguration configuration, bool redact, bool skipNotPresent = false, bool showSources = false)
        {
            Stream stream = new MemoryStream();

            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: !redact, skipNotPresent: skipNotPresent, showSources: showSources);
            jsonWriter.Dispose();

            stream.Position = 0;

            using (var streamReader = new StreamReader(stream))
            {
                string configString = streamReader.ReadToEnd();

                _outputHelper.WriteLine(configString);

                return configString;
            }
        }

        private static string CleanWhitespace(string rawText)
        {
            return string.Concat(rawText.Where(c => !char.IsWhiteSpace(c)));
        }

        private static string ConstructSettingsJson(params string[] permittedFileNames)
        {
            string[] filePaths = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SampleConfigurationsDirectory));

            IDictionary<string, JsonElement> combinedFiles = new Dictionary<string, JsonElement>();

            foreach (var filePath in filePaths)
            {
                if (!permittedFileNames.Any() || permittedFileNames.Contains(Path.GetFileName(filePath)))
                {
                    IDictionary<string, JsonElement> deserializedFile = JsonSerializer.Deserialize<IDictionary<string, JsonElement>>(File.ReadAllText(filePath));

                    foreach ((string key, JsonElement element) in deserializedFile)
                    {
                        combinedFiles.Add(key, element);
                    }
                }
            }

            string generatedUserSettings = JsonSerializer.Serialize(combinedFiles);

            return generatedUserSettings;
        }

        private static string ConstructExpectedOutput(bool redact, string directoryNameLocation)
        {
            Dictionary<string, string> categoryMapping = GetConfigurationFileNames(redact);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();

            foreach (var key in OrderedConfigurationKeys)
            {
                writer.WritePropertyName(key);

                if (categoryMapping.TryGetValue(key, out string fileName))
                {
                    string expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), directoryNameLocation, fileName);

                    writer.WriteRawValue(File.ReadAllText(expectedPath), skipInputValidation: true);
                }
                else
                {
                    writer.WriteStringValue(Strings.Placeholder_NotPresent);
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static Dictionary<string, string> GetConfigurationFileNames(bool redact)
        {
            return new Dictionary<string, string>()
            {
                { "GlobalCounter", "GlobalCounter.json" },
                { "Metrics", "Metrics.json" },
                { "Egress", redact ? "EgressRedacted.json" : "EgressFull.json" },
                { "Storage", "Storage.json" },
                { "urls", "URLs.json" },
                { "Logging", "Logging.json" },
                { "DefaultProcess", "DefaultProcess.json" },
                { "DiagnosticPort", "DiagnosticPort.json" },
                { "CollectionRules", "CollectionRules.json" },
                { "CollectionRuleDefaults", "CollectionRuleDefaults.json" },
                { "Templates", "Templates.json" },
                { "Authentication", redact ? "AuthenticationRedacted.json" : "AuthenticationFull.json" },
                { "InProcessFeatures", "InProcessFeatures.json" },
                { "DotnetMonitorDebug", "DotnetMonitorDebug.json" },
            };
        }

        public static IEnumerable<object[]> GetConnectionModeTestArguments()
        {
            yield return new object[] { "SimplifiedListen.txt", DiagnosticPortTestsConstants.SimplifiedListen_EnvironmentVariables };
            yield return new object[] { "FullListen.txt", DiagnosticPortTestsConstants.FullListen_EnvironmentVariables };
            yield return new object[] { "Connect.txt", DiagnosticPortTestsConstants.Connect_EnvironmentVariables };
            yield return new object[] { "SimplifiedListen.txt", DiagnosticPortTestsConstants.AllListen_EnvironmentVariables };
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
            MonitorEnvironment,
            UserProvidedFileSettings
        }
    }
}
