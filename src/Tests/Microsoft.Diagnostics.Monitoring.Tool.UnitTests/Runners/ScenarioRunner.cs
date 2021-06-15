using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    internal static class ScenarioRunner
    {
        public static async Task SingleTarget(
            ITestOutputHelper outputHelper,
            IHttpClientFactory httpClientFactory,
            DiagnosticPortConnectionMode mode,
            string scenarioName,
            Func<AppRunner, ApiClient, Task> callback)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(httpClientFactory);
            ApiClient apiClient = new(outputHelper, httpClient);

            AppRunner appRunner = new(outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = scenarioName;

            await appRunner.ExecuteAsync(async () =>
            {
                await callback(appRunner, apiClient);
            });
            Assert.Equal(0, appRunner.ExitCode);
        }
    }
}
