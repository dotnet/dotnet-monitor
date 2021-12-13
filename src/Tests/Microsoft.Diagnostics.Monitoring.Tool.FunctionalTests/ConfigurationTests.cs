// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class ConfigurationTests
    {
        private readonly ITestOutputHelper _outputHelper;

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

        private async Task RunConfigurationTest(bool redact, string userFileName, ConfigurationTestingMode testingMode, string expectedConfiguration)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = userFileName;
            toolRunner.TestingMode = testingMode;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FullConfigurationTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "UserSettings.json";
            toolRunner.TestingMode = ConfigurationTestingMode.All;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            if (redact)
            {
                CompareOutput(toolRunner._configurationString, configurationRedactedExpected);
            }
            else
            {
                CompareOutput(toolRunner._configurationString, configurationFullExpected);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CollectionRulesTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "CollectionRules.json";
            toolRunner.TestingMode = ConfigurationTestingMode.CollectionRules;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, ConstructExpectedOutput(collectionRulesExpected));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MetricsTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "Metrics.json";
            toolRunner.TestingMode = ConfigurationTestingMode.Metrics;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, ConstructExpectedOutput(metricsExpected));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AuthenticationTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(redact ? authenticationRedactedExpected : authenticationFullExpected);
            await RunConfigurationTest(redact, "Authentication.json", ConfigurationTestingMode.Authentication, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DefaultProcessTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(defaultProcessExpected);
            await RunConfigurationTest(redact, "DefaultProcess.json", ConfigurationTestingMode.DefaultProcess, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StorageTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(storageExpected);
            await RunConfigurationTest(redact, "Storage.json", ConfigurationTestingMode.Storage, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UrlsTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(urlsExpected);
            await RunConfigurationTest(redact, "URLs.json", ConfigurationTestingMode.URLs, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EgressTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(redact ? egressRedactedExpected : egressFullExpected);
            await RunConfigurationTest(redact, "Egress.json", ConfigurationTestingMode.Egress, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoggingTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(loggingExpected);
            await RunConfigurationTest(redact, "Logging.json", ConfigurationTestingMode.Logging, expectedConfiguration);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DiagnosticPortTest(bool redact)
        {
            string expectedConfiguration = ConstructExpectedOutput(diagnosticPortExpected);
            await RunConfigurationTest(redact, "DiagnosticPort.json", ConfigurationTestingMode.DiagnosticPort, expectedConfiguration);
        }

        private void CompareOutput(string actual, string expected)
        {
            string expectedCleaned = CleanOutput(expected);
            string actualCleaned = CleanOutput(actual);

            Assert.Equal(expectedCleaned, actualCleaned);
        }

        private string ConstructExpectedOutput(Dictionary<string, string> categoryMapping)
        {
            string expectedOutput = "{";

            int keyCount = 0;

            foreach (var key in orderedConfigurationKeys)
            {
                expectedOutput += "\"" + key + "\"" + ":";

                if (categoryMapping.ContainsKey(key))
                {
                    expectedOutput += categoryMapping[key];
                }
                else
                {
                    expectedOutput += "\":NOT PRESENT:\"";
                }

                if (keyCount != orderedConfigurationKeys.Count - 1)
                {
                    expectedOutput += ",";
                }

                ++keyCount;
            }

            expectedOutput += "}";

            return expectedOutput;
        }

        private string CleanOutput(string rawOutput)
        {
            return rawOutput.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        }

        string configurationFullExpected = @"
        {
            ""urls"": ""https://localhost:33333"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": {
              ""IntervalSeconds"": ""1""
            },
            ""CollectionRules"": {
              ""AspnetStatus"": {
                ""Actions"": [
                  {
                    ""Name"": ""MyDump"",
                    ""Settings"": {
                      ""Egress"": ""artifacts"",
                      ""Type"": ""Triage""
                    },
                    ""Type"": ""CollectDump"",
                    ""WaitForCompletion"": ""True""
                  },
                  {
                    ""Settings"": {
                      ""Arguments"": ""\u0022$(Actions.MyDump.EgressPath)\u0022"",
                      ""Path"": ""C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe""
                    },
                    ""Type"": ""Execute""
                  }
                ],
                ""Filters"": [
                  {
                    ""Key"": ""ProcessName"",
                    ""MatchType"": ""Exact"",
                    ""Value"": ""dotnet""
                  }
                ],
                ""Limits"": {
                  ""ActionCount"": ""2"",
                  ""ActionCountSlidingWindowDuration"": ""1:00:00""
                },
                ""Trigger"": {
                  ""Settings"": {
                    ""ResponseCount"": ""1"",
                    ""StatusCodes"": [
                      ""200-202""
                    ]
                  },
                  ""Type"": ""AspNetResponseStatus""
                }
              }
            },
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": {
              ""ConnectionMode"": ""Listen"",
              ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
            },
            ""Metrics"": {
              ""Enabled"": ""True"",
              ""Endpoints"": ""http://localhost:55555"",
              ""IncludeDefaultProviders"": ""False"",
              ""MetricCount"": ""2"",
              ""Providers"": [
                {
                  ""CounterNames"": [
                    ""connections-per-second""
                  ],
                  ""ProviderName"": ""Microsoft-AspNetCore-Server-Kestrel""
                }
              ]
            },
            ""Storage"": {
              ""DumpTempFolder"": ""/ephemeral-directory/""
            },
            ""DefaultProcess"": {
              ""Filters"": [
                {
                  ""Key"": ""ProcessName"",
                  ""Value"": ""MyProcess""
                }
              ]
            },
            ""Logging"": {
              ""Logging"": {
                ""LogLevel"": {
                  ""Default"": ""Information"",
                  ""Microsoft"": ""Warning"",
                  ""Microsoft.Hosting.Lifetime"": ""Information""
                }
              }
            },
            ""Authentication"": {
              ""MonitorApiKey"": {
                ""PublicKey"": ""eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"",
                ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff""
              }
            },
            ""Egress"": {
              ""AzureBlobStorage"": {
                ""monitorBlob"": {
                  ""accountKeyName"": ""MonitorBlobAccountKey"",
                  ""accountUri"": ""https://exampleaccount.blob.core.windows.net"",
                  ""blobPrefix"": ""artifacts"",
                  ""containerName"": ""dotnet-monitor""
                }
              },
              ""Properties"": {
                ""MonitorBlobAccountKey"": ""accountKey""
              }
            }
        }";

        string configurationRedactedExpected = @"
        {
            ""urls"": ""https://localhost:33333"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": {
              ""IntervalSeconds"": ""1""
            },
            ""CollectionRules"": {
              ""AspnetStatus"": {
                ""Actions"": [
                  {
                    ""Name"": ""MyDump"",
                    ""Settings"": {
                      ""Egress"": ""artifacts"",
                      ""Type"": ""Triage""
                    },
                    ""Type"": ""CollectDump"",
                    ""WaitForCompletion"": ""True""
                  },
                  {
                    ""Settings"": {
                      ""Arguments"": ""\u0022$(Actions.MyDump.EgressPath)\u0022"",
                      ""Path"": ""C:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\Common7\\IDE\\devenv.exe""
                    },
                    ""Type"": ""Execute""
                  }
                ],
                ""Filters"": [
                  {
                    ""Key"": ""ProcessName"",
                    ""MatchType"": ""Exact"",
                    ""Value"": ""dotnet""
                  }
                ],
                ""Limits"": {
                  ""ActionCount"": ""2"",
                  ""ActionCountSlidingWindowDuration"": ""1:00:00""
                },
                ""Trigger"": {
                  ""Settings"": {
                    ""ResponseCount"": ""1"",
                    ""StatusCodes"": [
                      ""200-202""
                    ]
                  },
                  ""Type"": ""AspNetResponseStatus""
                }
              }
            },
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": {
              ""ConnectionMode"": ""Listen"",
              ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
            },
            ""Metrics"": {
              ""Enabled"": ""True"",
              ""Endpoints"": ""http://localhost:55555"",
              ""IncludeDefaultProviders"": ""False"",
              ""MetricCount"": ""2"",
              ""Providers"": [
                {
                  ""CounterNames"": [
                    ""connections-per-second""
                  ],
                  ""ProviderName"": ""Microsoft-AspNetCore-Server-Kestrel""
                }
              ]
            },
            ""Storage"": {
              ""DumpTempFolder"": ""/ephemeral-directory/""
            },
            ""DefaultProcess"": {
              ""Filters"": [
                {
                  ""Key"": ""ProcessName"",
                  ""Value"": ""MyProcess""
                }
              ]
            },
            ""Logging"": {
              ""Logging"": {
                ""LogLevel"": {
                  ""Default"": ""Information"",
                  ""Microsoft"": ""Warning"",
                  ""Microsoft.Hosting.Lifetime"": ""Information""
                }
              }
            },
            ""Authentication"": {
              ""MonitorApiKey"": {
                ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff"",
                ""PublicKey"": "":REDACTED:""
              }
            },
            ""Egress"": {
              ""Properties"": {
                ""MonitorBlobAccountKey"": "":REDACTED:""
              },
              ""AzureBlobStorage"": {
                ""monitorBlob"": {
                  ""AccountUri"": ""https://exampleaccount.blob.core.windows.net"",
                  ""BlobPrefix"": ""artifacts"",
                  ""ContainerName"": ""dotnet-monitor"",
                  ""CopyBufferSize"": "":NOT PRESENT:"",
                  ""SharedAccessSignature"": "":NOT PRESENT:"",
                  ""AccountKey"": "":NOT PRESENT:"",
                  ""SharedAccessSignatureName"": "":NOT PRESENT:"",
                  ""AccountKeyName"": ""MonitorBlobAccountKey""
                }
              },
              ""FileSystem"": "":NOT PRESENT:""
            }
        }";

        Dictionary<string, string> metricsExpected = new()
        {
            { "GlobalCounter",
                @"{
                  ""IntervalSeconds"": ""5""
                }"
            },
            { "Metrics",
                @"{
                  ""Enabled"": ""True"",
                  ""Endpoints"": ""http://localhost:52325"",
                  ""IncludeDefaultProviders"": ""True"",
                  ""MetricCount"": ""10"",
                  ""Providers"": [
                    {
                      ""CounterNames"": [
                        ""connections-per-second"",
                        ""total-connections""
                      ],
                      ""ProviderName"": ""Microsoft-AspNetCore-Server-Kestrel""
                    }
                  ]
                }"
            }
        };

        Dictionary<string, string> egressFullExpected = new()
        {
            {
                "Egress",
                @"{
                  ""FileSystem"": {
                    ""artifacts"": {
                      ""directoryPath"": ""/artifacts""
                    }
                  }
                }"
            }
        };

        Dictionary<string, string> egressRedactedExpected = new()
        {
            {
                "Egress",
                @"{
                  ""Properties"": "":NOT PRESENT:"",
                  ""AzureBlobStorage"": "":NOT PRESENT:"",
                  ""FileSystem"": {
                    ""artifacts"": {
                      ""DirectoryPath"": ""/artifacts"",
                      ""IntermediateDirectoryPath"": "":NOT PRESENT:"",
                      ""CopyBufferSize"": "":NOT PRESENT:""
                    }
                  }
                }"
            }
        };

        Dictionary<string, string> storageExpected = new()
        {
            {"Storage",
                @"{
                  ""DumpTempFolder"": ""/ephemeral-directory/""
                }"
            }
        };

        Dictionary<string, string> urlsExpected = new()
        {
            { "urls", "\"https://localhost:44444\"" }
        };

        Dictionary<string, string> loggingExpected = new()
        {
            { "Logging", 
                @"{
                  ""CaptureScopes"": ""True"",
                  ""Console"": {
                    ""FormatterOptions"": {
                      ""ColorBehavior"": ""Default""
                    },
                    ""LogToStandardErrorThreshold"": ""Error""
                  }
                }"
            }
        };

        Dictionary<string, string> defaultProcessExpected = new()
        {
            { "DefaultProcess",
                @"{
                  ""Filters"": [
                    {
                      ""Key"": ""ProcessID"",
                      ""Value"": ""12345""
                    }
                  ]
                }"
            }
        };

        Dictionary<string, string> diagnosticPortExpected = new()
        {
            { "DiagnosticPort",
                @"{
                  ""ConnectionMode"": ""Listen"",
                  ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
                }"
            }
        };

        Dictionary<string, string> collectionRulesExpected = new()
        {
            {
                "CollectionRules",
                @"{
                  ""LargeGCHeap"": {
                    ""Actions"": [
                      {
                        ""Settings"": {
                          ""Egress"": ""artifacts""
                        },
                        ""Type"": ""CollectGCDump""
                      }
                    ],
                    ""Filters"": [
                      {
                        ""Key"": ""ProcessName"",
                        ""Value"": ""FirstWebApp""
                      }
                    ],
                    ""Limits"": {
                      ""ActionCount"": ""2"",
                      ""ActionCountSlidingWindowDuration"": ""1:00:00""
                    },
                    ""Trigger"": {
                      ""Settings"": {
                        ""CounterName"": ""gc-heap-size"",
                        ""GreaterThan"": ""10"",
                        ""ProviderName"": ""System.Runtime""
                      },
                      ""Type"": ""EventCounter""
                    }
                  }
                }"
            }
        };

        Dictionary<string, string> authenticationFullExpected = new()
        {
            { "Authentication",
                @"{
                  ""MonitorApiKey"": {
                    ""PublicKey"": ""eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"",
                    ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff""
                  }
                }"
            }
        };

        Dictionary<string, string> authenticationRedactedExpected = new()
        {
            { "Authentication",
                @"{
                  ""MonitorApiKey"": {
                    ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff"",
                    ""PublicKey"": "":REDACTED:""
                  }
                }"
            }
        };
    }
}