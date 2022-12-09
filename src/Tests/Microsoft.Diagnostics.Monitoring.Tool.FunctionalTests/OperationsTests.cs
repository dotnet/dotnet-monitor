// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class OperationsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string FileProviderName = "files";

        public OperationsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new(outputHelper);
        }

        [Fact]
        public async Task OperationsTagsTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await appRunner.ProcessIdTask;
                    _ = await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    string tagsQuery1 = "tag1,,tag2"; // Note that the extra comma is intentional
                    string tagsQuery2 = "tag2,tag3,tag4";

                    HashSet<string> tagsSet1 = new() { "tag1", "tag2" };
                    HashSet<string> tagsSet2 = new() { "tag2", "tag3", "tag4" };

                    OperationResponse response1 = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName, tagsQuery1);
                    OperationResponse response2 = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName, tagsQuery2);
                    await apiClient.CancelEgressOperation(response1.OperationUri);
                    await apiClient.CancelEgressOperation(response2.OperationUri);

                    List<ISet<string>> tags1Only = new() { tagsSet1 };
                    List<ISet<string>> tags2Only = new() { tagsSet2 };
                    List<ISet<string>> tagsAll = new() { tagsSet1, tagsSet2 };
                    List<ISet<string>> tagsEmpty = new() { };

                    ValidateResult(await apiClient.GetOperations(), tagsAll);
                    ValidateResult(await apiClient.GetOperations("tag1"), tags1Only);
                    ValidateResult(await apiClient.GetOperations("tag1,tag2"), tags1Only);
                    ValidateResult(await apiClient.GetOperations("tag1,,tag2"), tags1Only);
                    ValidateResult(await apiClient.GetOperations("tag3"), tags2Only);
                    ValidateResult(await apiClient.GetOperations("unknowntag"), tagsEmpty);
                    ValidateResult(await apiClient.GetOperations("tag1,tag3"), tagsEmpty);
                    ValidateResult(await apiClient.GetOperations(","), tagsAll);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        private static void ValidateResult(List<OperationSummary> actualOperations, List<ISet<string>> expectedOperationsTags)
        {
            Assert.Equal(actualOperations.Count, expectedOperationsTags.Count);

            for (int index = 0; index < actualOperations.Count; ++index)
            {
                Assert.Equal(expectedOperationsTags[index], actualOperations[index].Tags);
            }
        }
    }
}
