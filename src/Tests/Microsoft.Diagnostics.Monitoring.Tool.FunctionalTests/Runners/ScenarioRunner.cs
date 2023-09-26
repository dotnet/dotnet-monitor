﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    internal static class ScenarioRunner
    {
        public static async Task SingleTarget(
            ITestOutputHelper outputHelper,
            IHttpClientFactory httpClientFactory,
            DiagnosticPortConnectionMode mode,
            string scenarioName,
            Func<AppRunner, ApiClient, Task> appValidate,
            Func<ApiClient, int, Task> postAppValidate = null,
            Action<AppRunner> configureApp = null,
            Action<MonitorCollectRunner> configureTool = null,
            bool disableHttpEgress = false,
            LogLevel profilerLogLevel = LogLevel.Error,
            string subScenarioName = null)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(outputHelper);
            toolRunner.ConnectionModeViaCommandLine = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            toolRunner.DisableHttpEgress = disableHttpEgress;

            configureTool?.Invoke(toolRunner);

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(httpClientFactory);
            ApiClient apiClient = new(outputHelper, httpClient);

            await using AppRunner appRunner = new(outputHelper, Assembly.GetExecutingAssembly());
            if (profilerLogLevel != LogLevel.None)
            {
                appRunner.ProfilerLogLevel = profilerLogLevel.ToString("G");
            }
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = scenarioName;
            appRunner.SubScenarioName = subScenarioName;

            configureApp?.Invoke(appRunner);

            await appRunner.ExecuteAsync(async () =>
            {
                // Wait for the process to be discovered.
                int processId = await appRunner.ProcessIdTask;
                _ = await apiClient.GetProcessWithRetryAsync(outputHelper, pid: processId);

                await appValidate(appRunner, apiClient);
            });
            Assert.Equal(0, appRunner.ExitCode);

            if (null != postAppValidate)
            {
                await postAppValidate(apiClient, await appRunner.ProcessIdTask);
            }
        }
    }
}
