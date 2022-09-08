// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class StackTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        private const string ExpectedModule = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.dll";
        private const string ExpectedClass = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.StacksWorker+StacksWorkerNested`1[System.Int32]";
        private const string ExpectedFunction = @"DoWork[System.Int64]";

        public StackTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Fact]
        public Task TestPlainTextStacks()
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, plainText: true);
                    Assert.NotNull(holder);

                    using StreamReader reader = new StreamReader(holder.Stream);
                    string result = await reader.ReadToEndAsync();

                    string ExpectedFrame = FormattableString.Invariant($"{ExpectedModule}!{ExpectedClass}.{ExpectedFunction}");

                    Assert.Contains(ExpectedFrame, result);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                },
                configureTool: runner =>
                {
                });
        }

        [Fact]
        public Task TestJsonStacks()
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, plainText: false);
                    Assert.NotNull(holder);


                    WebApi.Models.StackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.StackResult>(holder.Stream);

                    bool foundStack = false;

                    foreach (WebApi.Models.Stack stack in result.Stacks)
                    {
                        if (stack.Frames.Any( f => f.ModuleName == ExpectedModule && f.ClassName == ExpectedClass && f.MethodName == ExpectedFunction))
                        {
                            foundStack = true;
                        }
                    }

                    Assert.True(foundStack);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                },
                configureTool: runner =>
                {
                });
        }
    }
}
