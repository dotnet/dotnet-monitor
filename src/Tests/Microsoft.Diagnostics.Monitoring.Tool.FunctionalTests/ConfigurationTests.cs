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

            Assert.Equal(settingsBaselineOutput.Replace(" ","").Replace("\n",""), toolRunner._configurationString.Replace(" ","").Replace("\n",""));

        }

        // Need to actually put real values here.
        string settings1Full = "{\"Metrics\": {\"Enabled\": \"True\",\"Endpoints\": \"http://localhost:52325\",\"IncludeDefaultProviders\": \"True\",\"MetricCount\": \"10\",\"Providers\": [{\"ProviderName\": \"Microsoft-AspNetCore-Server-Kestrel\",\"CounterNames\": [\"connections-per-second\",\"total-connections\"]}]},\"ApiAuthentication\": {\"ApiKeyHash\": \"5BEB39D01D65BA138493A0E95E1EFCF6DCE55B24CDDF5F10255796FD74455CF6\",\"ApiKeyHashType\": \"SHA256\"},\"DefaultProcess\": {\"Filters\": [{\"Key\": \"ProcessID\",\"Value\": \"12345\"}]},\"Egress\": {\"FileSystem\": {\"artifacts\": {\"directoryPath\": \"artifacts\"}}},\"Logging\": {\"CaptureScopes\": true,\"Console\": {\"FormatterOptions\": {\"ColorBehavior\": \"Default\"},\"LogToStandardErrorThreshold\": \"Error\"}},\"CollectionRules\": {\"LargeGCHeap\": {\"Trigger\": {\"Type\": \"EventCounter\",\"Settings\": {\"ProviderName\": \"System.Runtime\",\"CounterName\": \"gc-heap-size\",\"GreaterThan\": 10}},\"Actions\": [{\"Type\": \"CollectGCDump\",\"Settings\": {\"Egress\": \"artifacts\"}}]}},\"DiagnosticPort\": {\"ConnectionMode\": \"Listen\",\"EndpointName\": \"\\\\.\\pipe\\dotnet-monitor-pipe\"}}";

        // Need to actually put real values here.
        string settings1Redact = "{\"Metrics\": {\"Enabled\": \"True\",\"Endpoints\": \"http://localhost:52325\",\"IncludeDefaultProviders\": \"True\",\"MetricCount\": \"10\",\"Providers\": [{\"ProviderName\": \"Microsoft-AspNetCore-Server-Kestrel\",\"CounterNames\": [\"connections-per-second\",\"total-connections\"]}]},\"ApiAuthentication\": {\"ApiKeyHash\": \"5BEB39D01D65BA138493A0E95E1EFCF6DCE55B24CDDF5F10255796FD74455CF6\",\"ApiKeyHashType\": \"SHA256\"},\"DefaultProcess\": {\"Filters\": [{\"Key\": \"ProcessID\",\"Value\": \"12345\"}]},\"Egress\": {\"FileSystem\": {\"artifacts\": {\"directoryPath\": \"artifacts\"}}},\"Logging\": {\"CaptureScopes\": true,\"Console\": {\"FormatterOptions\": {\"ColorBehavior\": \"Default\"},\"LogToStandardErrorThreshold\": \"Error\"}},\"CollectionRules\": {\"LargeGCHeap\": {\"Trigger\": {\"Type\": \"EventCounter\",\"Settings\": {\"ProviderName\": \"System.Runtime\",\"CounterName\": \"gc-heap-size\",\"GreaterThan\": 10}},\"Actions\": [{\"Type\": \"CollectGCDump\",\"Settings\": {\"Egress\": \"artifacts\"}}]}},\"DiagnosticPort\": {\"ConnectionMode\": \"Listen\",\"EndpointName\": \"\\\\.\\pipe\\dotnet-monitor-pipe\"}}";
    }
}