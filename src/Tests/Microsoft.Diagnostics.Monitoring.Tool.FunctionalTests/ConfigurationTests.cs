// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
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
        public async Task ConfigShowTest(bool redact)
        {
            await using MonitorConfigRunner toolRunner = new(_outputHelper);
            toolRunner.Redact = redact;
            toolRunner.FileName = "Settings1.json";
            await toolRunner.StartAsync();

            string settingsBaselineOutput = redact ? settings1Redact : settings1Full;

            Assert.Equal(settingsBaselineOutput.Replace(" ","").Replace("\n","").Replace("\r",""), toolRunner._configurationString.Replace(" ","").Replace("\n","").Replace("\r", ""));
        }

        string settings1Full = @"
        {
          ""urls"": ""https://localhost:52323"",
          ""Kestrel"": "":NOT PRESENT:"",
          ""GlobalCounter"": {
            ""IntervalSeconds"": ""5""
          },
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
          ""DiagnosticPort"": {
            ""ConnectionMode"": ""Listen"",
            ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
          },
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
          ""Storage"": {
            ""DumpTempFolder"": ""C:\\Users\\kkeirstead\\AppData\\Local\\Temp\\""
          },
          ""DefaultProcess"": {
            ""Filters"": [
              {
                ""Key"": ""ProcessID"",
                ""Value"": ""12345""
              }
            ]
          },
          ""Logging"": {
            ""CaptureScopes"": ""True"",
            ""Console"": {
              ""FormatterName"": ""simple"",
              ""FormatterOptions"": {
                ""ColorBehavior"": ""Default"",
                ""IncludeScopes"": ""True"",
                ""TimestampFormat"": ""HH:mm:ss ""
              },
              ""LogToStandardErrorThreshold"": ""Error""
            },
            ""EventLog"": {
              ""LogLevel"": {
                ""Default"": ""Information"",
                ""Microsoft"": ""Warning"",
                ""Microsoft.Diagnostics"": ""Information"",
                ""Microsoft.Hosting.Lifetime"": ""Information""
              }
            },
            ""LogLevel"": {
              ""Default"": ""Information"",
              ""Microsoft"": ""Warning"",
              ""Microsoft.Diagnostics"": ""Information"",
              ""Microsoft.Hosting.Lifetime"": ""Information""
            }
          },
          ""Authentication"": {
            ""MonitorApiKey"": {
              ""PublicKey"": ""eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19"",
              ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff""
            }
          },
          ""Egress"": {
            ""FileSystem"": {
              ""artifacts"": {
                ""directoryPath"": ""/artifacts""
              }
            }
          }
        }";

        string settings1Redact = @"
        {
          ""urls"": ""https://localhost:52323"",
          ""Kestrel"": "":NOT PRESENT:"",
          ""GlobalCounter"": {
            ""IntervalSeconds"": ""5""
          },
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
          ""DiagnosticPort"": {
            ""ConnectionMode"": ""Listen"",
            ""EndpointName"": ""\\\\.\\pipe\\dotnet-monitor-pipe""
          },
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
          ""Storage"": {
            ""DumpTempFolder"": ""C:\\Users\\kkeirstead\\AppData\\Local\\Temp\\""
          },
          ""DefaultProcess"": {
            ""Filters"": [
              {
                ""Key"": ""ProcessID"",
                ""Value"": ""12345""
              }
            ]
          },
          ""Logging"": {
            ""CaptureScopes"": ""True"",
            ""Console"": {
              ""FormatterName"": ""simple"",
              ""FormatterOptions"": {
                ""ColorBehavior"": ""Default"",
                ""IncludeScopes"": ""True"",
                ""TimestampFormat"": ""HH:mm:ss ""
              },
              ""LogToStandardErrorThreshold"": ""Error""
            },
            ""EventLog"": {
              ""LogLevel"": {
                ""Default"": ""Information"",
                ""Microsoft"": ""Warning"",
                ""Microsoft.Diagnostics"": ""Information"",
                ""Microsoft.Hosting.Lifetime"": ""Information""
              }
            },
            ""LogLevel"": {
              ""Default"": ""Information"",
              ""Microsoft"": ""Warning"",
              ""Microsoft.Diagnostics"": ""Information"",
              ""Microsoft.Hosting.Lifetime"": ""Information""
            }
          },
          ""Authentication"": {
            ""MonitorApiKey"": {
              ""Subject"": ""ae5473b6-8dad-498d-b915-ffffffffffff"",
              ""PublicKey"": "":REDACTED:""
            }
          },
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
    }
}