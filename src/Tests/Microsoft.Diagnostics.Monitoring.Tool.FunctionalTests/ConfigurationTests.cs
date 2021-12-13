// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class ConfigurationTests
    {
        private readonly ITestOutputHelper _outputHelper;
        public ConfigurationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
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

            CompareOutput(toolRunner._configurationString, collectionRulesExpected);
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

            CompareOutput(toolRunner._configurationString, metricsExpected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AuthenticationTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "Authentication.json";
            toolRunner.TestingMode = ConfigurationTestingMode.Authentication;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            if (redact)
            {
                CompareOutput(toolRunner._configurationString, authenticationRedactedExpected);
            }
            else
            {
                CompareOutput(toolRunner._configurationString, authenticationFullExpected);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DefaultProcessTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "DefaultProcess.json";
            toolRunner.TestingMode = ConfigurationTestingMode.DefaultProcess;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, defaultProcessExpected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StorageTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "Storage.json";
            toolRunner.TestingMode = ConfigurationTestingMode.Storage;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, storageExpected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UrlsTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "URLs.json";
            toolRunner.TestingMode = ConfigurationTestingMode.URLs;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, urlsExpected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EgressTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "Egress.json";
            toolRunner.TestingMode = ConfigurationTestingMode.Egress;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();
            
            if (redact)
            {
                CompareOutput(toolRunner._configurationString, egressRedactedExpected);
            }
            else
            {
                CompareOutput(toolRunner._configurationString, egressFullExpected);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoggingTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "Logging.json";
            toolRunner.TestingMode = ConfigurationTestingMode.Logging;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, loggingExpected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DiagnosticPortTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.UserFileName = "DiagnosticPort.json";
            toolRunner.TestingMode = ConfigurationTestingMode.DiagnosticPort;
            toolRunner.SharedFileName = "SharedSettings.json";

            await toolRunner.StartAsync();

            CompareOutput(toolRunner._configurationString, diagnosticPortExpected);
        }

        private void CompareOutput(string actual, string expected)
        {
            string expectedCleaned = CleanOutput(expected);
            string actualCleaned = CleanOutput(actual);

            Assert.Equal(expectedCleaned, actualCleaned);
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

        string metricsExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": {
              ""IntervalSeconds"": ""5""
            },
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": {
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
            },
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string egressFullExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": {
              ""FileSystem"": {
                ""artifacts"": {
                  ""directoryPath"": ""/artifacts""
                }
              }
            }
        }";

        string egressRedactedExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": {
              ""Properties"": "":NOT PRESENT:"",
              ""AzureBlobStorage"": "":NOT PRESENT:"",
              ""FileSystem"": {
                ""artifacts"": {
                  ""DirectoryPath"": ""/artifacts"",
                  ""IntermediateDirectoryPath"": "":NOT PRESENT:"",
                  ""CopyBufferSize"": "":NOT PRESENT:""
                }
              }
            }
        }";

        string storageExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": {
              ""DumpTempFolder"": ""/ephemeral-directory/""
            },
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string urlsExpected = @"
        {
            ""urls"": ""https://localhost:44444"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string loggingExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": {
              ""CaptureScopes"": ""True"",
              ""Console"": {
                ""FormatterOptions"": {
                  ""ColorBehavior"": ""Default""
                },
                ""LogToStandardErrorThreshold"": ""Error""
              }
            },
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string defaultProcessExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": {
              ""Filters"": [
                {
                  ""Key"": ""ProcessID"",
                  ""Value"": ""12345""
                }
              ]
            },
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string diagnosticPortExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": {
              ""ConnectionMode"": ""Listen"",
              ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
            },
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string collectionRulesExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": {
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
            },
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": "":NOT PRESENT:"",
            ""Egress"": "":NOT PRESENT:""
        }";

        string authenticationFullExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": {
              ""MonitorApiKey"": {
                ""PublicKey"": ""eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"",
                ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff""
              }
            },
            ""Egress"": "":NOT PRESENT:""
        }";

        string authenticationRedactedExpected = @"
        {
            ""urls"": "":NOT PRESENT:"",
            ""Kestrel"": "":NOT PRESENT:"",
            ""GlobalCounter"": "":NOT PRESENT:"",
            ""CollectionRules"": "":NOT PRESENT:"",
            ""CorsConfiguration"": "":NOT PRESENT:"",
            ""DiagnosticPort"": "":NOT PRESENT:"",
            ""Metrics"": "":NOT PRESENT:"",
            ""Storage"": "":NOT PRESENT:"",
            ""DefaultProcess"": "":NOT PRESENT:"",
            ""Logging"": "":NOT PRESENT:"",
            ""Authentication"": {
              ""MonitorApiKey"": {
                ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff"",
                ""PublicKey"": "":REDACTED:""
              }
            },
            ""Egress"": "":NOT PRESENT:""
        }";
    }
}