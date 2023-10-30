// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class AzureAdIntegrationTests
    {
        private const string AzureAdTestEnvVariablePrefix = "DOTNET_MONITOR_AZURE_AD_TESTS_";
        private const string EnableTestsEnvVariable = $"{AzureAdTestEnvVariablePrefix}ENABLE";
        private const string TenantIdEnvVariable = $"{AzureAdTestEnvVariablePrefix}TENANT_ID";
        private const string PipelineAgentClientIdEnvVariable = $"{AzureAdTestEnvVariablePrefix}PIPELINE_CLIENT_ID";
        private const string PipelineAgentClientSecretEnvVariable = $"{AzureAdTestEnvVariablePrefix}PIPELINE_CLIENT_SECRET";
        private const string ClientIdEnvVariable = $"{AzureAdTestEnvVariablePrefix}CLIENT_ID";
        private const string RequiredRoleVariable = $"{AzureAdTestEnvVariablePrefix}REQUIRED_ROLE";


        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public AzureAdIntegrationTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [ConditionalFact]
        public async Task RejectsValidIdentityRequestWithoutCorrectRole()
        {
            SkipTestIfNotEnabled();

            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            AzureAdOptions azureAdOptions = GetAzureAdOptionsFromEnv();

            // Alter the required role so the pipeline agent token doesn't match
            azureAdOptions.RequiredRole = Guid.NewGuid().ToString("D");
            toolRunner.ConfigurationFromEnvironment.UseAzureAd(azureAdOptions);

            string accessToken = await GenerateAccessTokenAsync(azureAdOptions);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, accessToken);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                apiClient.GetProcessesAsync);
            Assert.Equal(HttpStatusCode.Forbidden, statusCodeException.StatusCode);
        }

        [ConditionalFact]
        public async Task CanAuthenticateWithCorrectRole()
        {
            SkipTestIfNotEnabled();

            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            AzureAdOptions azureAdOptions = GetAzureAdOptionsFromEnv();
            toolRunner.ConfigurationFromEnvironment.UseAzureAd(azureAdOptions);

            string accessToken = await GenerateAccessTokenAsync(azureAdOptions);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, accessToken);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);
        }

        private static void SkipTestIfNotEnabled()
        {
            const string disabledEnvVariableValue = "0";

            string enableEndToEndAzureAdTests = Environment.GetEnvironmentVariable(EnableTestsEnvVariable);
            if (string.IsNullOrEmpty(enableEndToEndAzureAdTests) || string.Equals(enableEndToEndAzureAdTests, disabledEnvVariableValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new SkipTestException($"AzureAd integration tests are not enabled.");
            }
        }

        private static string MustGetEnvironmentVariable(string envVariable)
        {
            string value = Environment.GetEnvironmentVariable(envVariable);
            Assert.False(string.IsNullOrWhiteSpace(value), $"Environment variable '{envVariable}' is not set");

            return value;
        }

        private static async Task<string> GenerateAccessTokenAsync(AzureAdOptions options)
        {
            string agentClientId = MustGetEnvironmentVariable(PipelineAgentClientIdEnvVariable);
            string agentClientSecret = MustGetEnvironmentVariable(PipelineAgentClientSecretEnvVariable);

            string preConfiguredScope = new Uri(options.GetAppIdUri(), ".default").ToString();

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(agentClientId)
                .WithClientSecret(agentClientSecret)
                .WithAuthority(new Uri(options.GetInstance(), options.TenantId))
                .Build();

            AuthenticationResult authResult = await app.AcquireTokenForClient(new[] { preConfiguredScope }).ExecuteAsync().ConfigureAwait(false);
            Assert.NotNull(authResult.AccessToken);

            return authResult.AccessToken;
        }

        private static AzureAdOptions GetAzureAdOptionsFromEnv()
        {
            string tenantId = MustGetEnvironmentVariable(TenantIdEnvVariable);
            string clientId = MustGetEnvironmentVariable(ClientIdEnvVariable);
            string requiredRole = MustGetEnvironmentVariable(RequiredRoleVariable);

            return new AzureAdOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                RequiredRole = requiredRole
            };
        }
    }
}
