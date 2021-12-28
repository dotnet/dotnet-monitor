// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class ConfigurationTests
    {
        private readonly ITestOutputHelper _outputHelper;

        // This needs to be updated and kept in order for any future configuration sections
        private readonly List<string> orderedConfigurationKeys = new()
        {
            "urls",
            "Kestrel",
            "GlobalCounter",
            "CollectionRules",
            "CorsConfiguration",
            "DiagnosticPort",
            "Metrics",
            "Storage",
            "DefaultProcess",
            "Logging",
            "Authentication",
            "Egress"
        };

        public ConfigurationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private void RunConfigurationTest(bool redact, string userFileName, ConfigurationTestingMode testingMode, string expectedConfiguration)
        {
            string userSettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleConfigurations", userFileName);

            Stream stream = new MemoryStream();
            ConfigShowCommandHandler.Write(stream, System.Array.Empty<string>(), System.Array.Empty<string>(), false, "", false, false, redact ? ConfigDisplayLevel.Redacted : ConfigDisplayLevel.Full, userSettingsFilePath, testingMode);

            stream.Position = 0;

            string configString = "";
            using (var streamReader = new StreamReader(stream))
            {
                configString = streamReader.ReadToEnd();
            }

            _outputHelper.WriteLine(configString);

            CompareOutput(configString, expectedConfiguration);


            /*
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = userFileName;
            toolRunner.TestingMode = testingMode;

            await toolRunner.StartAsync();

            CompareOutput(toolRunner.ConfigurationString, expectedConfiguration);
            */

        }

        /// <summary>
        /// Instead of having to explicitly define every expected value, this reuses the individual categories to ensure they
        /// assemble properly when combined. NOTE: The UserSettings.json file must follow what's included in the
        /// other json files; thus, if one of them is updated, UserSettings.json should also be updated to reflect the change.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FullConfigurationTest(bool redact)
        {
            List<Dictionary<string, string>> configurations = new();

            // NOTE: This should include each category that's we're testing -> if more are added, they should be added here as well.
            configurations.Add(metricsExpected);
            configurations.Add(loggingExpected);
            configurations.Add(defaultProcessExpected);
            configurations.Add(diagnosticPortExpected);
            configurations.Add(redact ? egressRedactedExpected : egressFullExpected);
            configurations.Add(storageExpected);
            configurations.Add(urlsExpected);
            configurations.Add(redact ? authenticationRedactedExpected : authenticationFullExpected);
            configurations.Add(collectionRulesExpected);

            Dictionary<string, string> fullConfiguration = configurations.SelectMany(x => x).ToDictionary(x => x.Key, y => y.Value);

            string expectedConfiguration = ConstructExpectedOutput(fullConfiguration);
            RunConfigurationTest(redact, "UserSettings.json", ConfigurationTestingMode.All, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CollectionRulesTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(collectionRulesExpected);
            RunConfigurationTest(redact, "CollectionRules.json", ConfigurationTestingMode.CollectionRules, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MetricsTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(metricsExpected);
            RunConfigurationTest(redact, "Metrics.json", ConfigurationTestingMode.Metrics, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AuthenticationTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(redact ? authenticationRedactedExpected : authenticationFullExpected);
            RunConfigurationTest(redact, "Authentication.json", ConfigurationTestingMode.Authentication, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DefaultProcessTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(defaultProcessExpected);
            RunConfigurationTest(redact, "DefaultProcess.json", ConfigurationTestingMode.DefaultProcess, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StorageTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(storageExpected);
            RunConfigurationTest(redact, "Storage.json", ConfigurationTestingMode.Storage, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UrlsTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(urlsExpected);
            RunConfigurationTest(redact, "URLs.json", ConfigurationTestingMode.URLs, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EgressTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(redact ? egressRedactedExpected : egressFullExpected);
            RunConfigurationTest(redact, "Egress.json", ConfigurationTestingMode.Egress, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoggingTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(loggingExpected);
            RunConfigurationTest(redact, "Logging.json", ConfigurationTestingMode.Logging, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DiagnosticPortTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(diagnosticPortExpected);
            RunConfigurationTest(redact, "DiagnosticPort.json", ConfigurationTestingMode.DiagnosticPort, expectedConfiguration);
        }

        private void CompareOutput(string actual, string expected)
        {
            Assert.Equal(CleanOutput(expected), CleanOutput(actual));
        }

        private string ConstructExpectedOutput(Dictionary<string, string> categoryMapping)
        {
            string expectedOutput = "{";

            foreach (var key in orderedConfigurationKeys)
            {
                expectedOutput += "\"" + key + "\"" + ":";

                if (categoryMapping.ContainsKey(key))
                {
                    string expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "ExpectedConfigurations", categoryMapping[key]);
                    expectedOutput += File.ReadAllText(expectedPath);
                }
                else
                {
                    expectedOutput += "\":NOT PRESENT:\"";
                }

                if (!key.Equals(orderedConfigurationKeys.Last()))
                {
                    expectedOutput += ",";
                }
            }

            expectedOutput += "}";

            return expectedOutput;
        }

        private string CleanOutput(string rawOutput)
        {
            return string.Concat(rawOutput.Where(c => !char.IsWhiteSpace(c)));
        }

        private Dictionary<string, string> metricsExpected = new()
        {
            { "GlobalCounter", "GlobalCounter.json" },
            { "Metrics", "Metrics.json" }
        };

        private Dictionary<string, string> egressFullExpected = new()
        {
            { "Egress", "EgressFull.json" }
        };

        private Dictionary<string, string> egressRedactedExpected = new()
        {
            { "Egress", "EgressRedacted.json" }
        };

        private Dictionary<string, string> storageExpected = new()
        {
            { "Storage", "Storage.json" }
        };

        private Dictionary<string, string> urlsExpected = new()
        {
            { "urls", "URLs.json" }
        };

        private Dictionary<string, string> loggingExpected = new()
        {
            { "Logging", "Logging.json" }
        };

        private Dictionary<string, string> defaultProcessExpected = new()
        {
            { "DefaultProcess", "DefaultProcess.json" }
        };

        private Dictionary<string, string> diagnosticPortExpected = new()
        {
            { "DiagnosticPort", "DiagnosticPort.json" }
        };

        private Dictionary<string, string> collectionRulesExpected = new()
        {
            { "CollectionRules", "CollectionRules.json"}
        };

        private Dictionary<string, string> authenticationFullExpected = new()
        {
            { "Authentication", "AuthenticationFull.json" }
        };

        private Dictionary<string, string> authenticationRedactedExpected = new()
        {
            { "Authentication", "AuthenticationRedacted.json" }
        };
    }
}
