﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class AuthenticationTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly List<(string fieldName, DateTime time)> _warnPrivateKeyLog = new();

        public AuthenticationTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests the behavior of the default URL address, namely that all routes require
        /// authentication (401 Unauthorized) except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task DefaultAddressTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Any authenticated route on the default address should 401 Unauthenticated

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 401 Unauthenticated

            // Metrics should not throw on the default address

            var metrics = await apiClient.GetMetricsAsync();
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests the behavior of the metrics URL address, namely that all routes will return
        /// 404 Not Found except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task MetricsAddressTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientMetricsAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Any non-metrics route on the metrics address should 404 Not Found
            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.NotFound, statusCodeException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 404 Not Found

            // Metrics should not throw on the metrics address
            var metrics = await apiClient.GetMetricsAsync();
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests that turning off authentication allows access to all routes without authentication.
        /// </summary>
        [Fact]
        public async Task DisableAuthenticationTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /processes does not challenge for authentication
            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);

            // Check that /metrics does not challenge for authentication
            var metrics = await apiClient.GetMetricsAsync();
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests that API key authentication can be configured correctly and
        /// that the key can be rotated without shutting down dotnet-monitor.
        /// </summary>
        [Fact]
        public async Task ApiKeyAuthenticationSchemeTest()
        {
            const string signingAlgo = "ES384";
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            // Set API key via key-per-file
            RootOptions options = new();
            options.UseApiKey(signingAlgo, Guid.NewGuid(), out string apiKey);
            toolRunner.WriteKeyPerValueConfiguration(options);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /processes does not challenge for authentication
            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);

            _outputHelper.WriteLine("Rotating API key.");

            // Rotate the API key
            options.UseApiKey(signingAlgo, Guid.NewGuid(), out string apiKey2);
            toolRunner.WriteKeyPerValueConfiguration(options);

            // Wait for the key rotation to be consumed by dotnet-monitor; detect this
            // by checking for when API returns a 401. Ideally, key rotation would write
            // log event and runner monitor for event and notify.
            int attempts = 0;
            int currMsDelay = 300;
            const int MaxDelayMs = 3000;
            while (true)
            {
                attempts++;
                _outputHelper.WriteLine("Waiting for key rotation (attempt #{0}).", attempts);

                await Task.Delay(TimeSpan.FromMilliseconds(currMsDelay));
                currMsDelay = Math.Min(MaxDelayMs, currMsDelay * 2);

                try
                {
                    await apiClient.GetProcessesAsync();
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    break;
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Forbidden means that the user authorization handler picked up the change but not user authentication handler.
                    // We need to continue to wait.
                }

                Assert.True(attempts < 10);
            }

            // check that the old key is now invalid
            ApiStatusCodeException thrownEx = await Assert.ThrowsAsync<ApiStatusCodeException>(async () => await apiClient.GetProcessesAsync());
            Assert.True(HttpStatusCode.Unauthorized == thrownEx.StatusCode);

            _outputHelper.WriteLine("Verifying new API key.");

            // Use new API key
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey2);

            // Check that /processes does not challenge for authentication
            processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);
        }

        /// <summary>
        /// Tests that limits on API key length and
        /// disallowed algorithms are enforced.
        /// </summary>
        [Theory]
        [InlineData(SecurityAlgorithms.EcdsaSha256, true)]
        [InlineData(SecurityAlgorithms.EcdsaSha256Signature, true)]
        [InlineData(SecurityAlgorithms.EcdsaSha384, true)]
        [InlineData(SecurityAlgorithms.EcdsaSha384Signature, true)]
        [InlineData(SecurityAlgorithms.EcdsaSha512, true)]
        [InlineData(SecurityAlgorithms.EcdsaSha512Signature, true)]
        [InlineData(SecurityAlgorithms.RsaSha256, true)]
        [InlineData(SecurityAlgorithms.RsaSha256Signature, true)]
        [InlineData(SecurityAlgorithms.RsaSha384, true)]
        [InlineData(SecurityAlgorithms.RsaSha384Signature, true)]
        [InlineData(SecurityAlgorithms.RsaSha512, true)]
        [InlineData(SecurityAlgorithms.RsaSha512Signature, true)]
        [InlineData(SecurityAlgorithms.HmacSha256, false)]
        [InlineData(SecurityAlgorithms.HmacSha384, false)]
        [InlineData(SecurityAlgorithms.HmacSha512, false)]
        public async Task ApiKeyAlgorithmTest(string signingAlgo, bool valid)
        {
            MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.WarnPrivateKey += ToolRunner_WarnPrivateKey;
            await using (toolRunner)
            {
                toolRunner.DisableMetricsViaCommandLine = true;

                _outputHelper.WriteLine("Generating API key.");

                toolRunner.ConfigurationFromEnvironment.UseApiKey(signingAlgo, Guid.NewGuid(), out string apiKey);

                // Start dotnet-monitor
                await toolRunner.StartAsync();

                // Create HttpClient with default request headers
                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
                ApiClient apiClient = new(_outputHelper, httpClient);

                if (valid)
                {
                    var processes = await apiClient.GetProcessesAsync();
                    Assert.NotNull(processes);
                }
                else
                {
                    ApiStatusCodeException ex = await Assert.ThrowsAsync<ApiStatusCodeException>(async () => { await apiClient.GetProcessesAsync(); });
                    Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
                }
            }
            toolRunner.WarnPrivateKey -= ToolRunner_WarnPrivateKey;

            Assert.Empty(_warnPrivateKeyLog);
        }

        /// <summary>
        /// This tests that a valid JWT with the correct subject ID 
        /// that is signed with a key other than the specified one will get rejected.
        /// </summary>
        [Theory]
        [InlineData(SecurityAlgorithms.EcdsaSha384)]
        [InlineData(SecurityAlgorithms.RsaSha384)]
        public async Task RejectsWrongSigningKey(string signingAlgo)
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            Guid subject = Guid.NewGuid();
            toolRunner.ConfigurationFromEnvironment.UseApiKey(signingAlgo, subject, out string apiKeyReal);

            _outputHelper.WriteLine("Getting fake API key.");
            // Regenerate a new JWT with the same sub but different signing key, don't update the config
            new RootOptions().UseApiKey(signingAlgo, subject, out string apiKeyFake);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKeyFake);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        /// <summary>
        /// This tests that a valid JWT is not accepted when not configured
        /// </summary>
        [Fact]
        public async Task RejectsApiKeyNotConfigured()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            // Get a apiKey, but throw away the config
            (new RootOptions()).UseApiKey(SecurityAlgorithms.EcdsaSha384, Guid.NewGuid(), out string apiKey);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        [Fact]
        public async Task RejectsBadAudience()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            Guid subject = Guid.NewGuid();
            string subjectStr = subject.ToString("D");
            const string BadApiKeyJwtAudience = "SomeOtherAudience";
            JwtPayload newPayload = GetJwtPayload(BadApiKeyJwtAudience, subjectStr, AuthConstants.ApiKeyJwtInternalIssuer);

            toolRunner.ConfigurationFromEnvironment.UseApiKey(SecurityAlgorithms.EcdsaSha384, subjectStr, newPayload, out string apiKey);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        [Fact]
        public async Task RejectsMissingAudience()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            Guid subject = Guid.NewGuid();
            string subjectStr = subject.ToString("D");
            JwtPayload newPayload = GetJwtPayload(null, subjectStr, AuthConstants.ApiKeyJwtInternalIssuer);

            toolRunner.ConfigurationFromEnvironment.UseApiKey(SecurityAlgorithms.EcdsaSha384, subjectStr, newPayload, out string apiKey);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        [Fact]
        public async Task AllowDifferentIssuer()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            Guid subject = Guid.NewGuid();
            string subjectStr = subject.ToString("D");
            const string ApiKeyJwtIssuer = "MyOtherServiceMintingTokens";
            JwtPayload newPayload = GetJwtPayload(AuthConstants.ApiKeyJwtAudience, subjectStr, ApiKeyJwtIssuer);

            toolRunner.ConfigurationFromEnvironment.UseApiKey(SecurityAlgorithms.EcdsaSha384, subjectStr, newPayload, out string apiKey);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);
        }

        /// <summary>
        /// This tests that invalid subject vs configured subject gets rejected. Any string is valid 
        /// but it must match between the 'sub' field in the jwt and the Subject configuration parameter.
        /// </summary>
        [Theory]
        // Guids that don't match should get rejected
        [InlineData("980d2b17-71e1-4313-a084-c077e962680c", "10253b7a-454d-41bb-a3f5-5f2e6b26ed93", HttpStatusCode.Forbidden)]
        // Empty string isn't valid even when signed and configured correctly
        [InlineData("", "", HttpStatusCode.Unauthorized)]
        [InlineData("10253b7a-454d-41bb-a3f5-5f2e6b26ed93", "", HttpStatusCode.Unauthorized)]
        [InlineData("", "10253b7a-454d-41bb-a3f5-5f2e6b26ed93", HttpStatusCode.Forbidden)]
        public async Task RejectsBadSubject(string jwtSubject, string configSubject, HttpStatusCode expectedError)
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;

            _outputHelper.WriteLine("Generating API key.");

            JwtPayload newPayload = GetJwtPayload(AuthConstants.ApiKeyJwtAudience, jwtSubject, AuthConstants.ApiKeyJwtInternalIssuer);

            toolRunner.ConfigurationFromEnvironment.UseApiKey(SecurityAlgorithms.EcdsaSha384, configSubject, newPayload, out string apiKey);

            // Start dotnet-monitor
            await toolRunner.StartAsync();

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            ApiClient apiClient = new(_outputHelper, httpClient);

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(expectedError, statusCodeException.StatusCode);
        }

        /// <summary>
        /// Tests that we get a warning message when a user provides a private key in the public key configuration.
        /// </summary>
        [Theory]
        [InlineData(SecurityAlgorithms.EcdsaSha384)]
        [InlineData(SecurityAlgorithms.RsaSha384)]
        public async Task WarnOnPrivateKey(string signingAlgo)
        {
            MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.WarnPrivateKey += ToolRunner_WarnPrivateKey;
            await using (toolRunner)
            {
                toolRunner.DisableMetricsViaCommandLine = true;

                _outputHelper.WriteLine("Generating API key.");

                Guid subject = Guid.NewGuid();
                string subjectStr = subject.ToString("D");
                JwtPayload newPayload = GetJwtPayload(AuthConstants.ApiKeyJwtAudience, subjectStr, AuthConstants.ApiKeyJwtInternalIssuer);
                RootOptions opts = new();
                opts.UseApiKey(signingAlgo, subjectStr, newPayload, out string apiKey, out SecurityKey creds);

                JsonWebKey exportableJwk = null;
                if (signingAlgo.StartsWith("RS"))
                {
                    exportableJwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(creds as RsaSecurityKey);
                }
                else if (signingAlgo.StartsWith("ES"))
                {
                    exportableJwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(creds as ECDsaSecurityKey);
                }
                else
                {
                    Assert.True(false, "Unknown algorithm");
                }

                JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonSerializerOptionsFactory.JsonIgnoreCondition.WhenWritingNull);
                serializerOptions.IgnoreReadOnlyProperties = true;
                string privateKeyJson = JsonSerializer.Serialize(exportableJwk, serializerOptions);
                string privateKeyEncoded = Base64UrlEncoder.Encode(privateKeyJson);

                AuthenticationOptions authOpts = new AuthenticationOptions()
                {
                    MonitorApiKey = new MonitorApiKeyOptions()
                    {
                        Subject = opts.Authentication.MonitorApiKey.Subject,
                        PublicKey = privateKeyEncoded,
                    },
                };
                toolRunner.ConfigurationFromEnvironment.Authentication = authOpts;

                // Start dotnet-monitor
                await toolRunner.StartAsync();

                // Create HttpClient with default request headers
                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
                ApiClient apiClient = new(_outputHelper, httpClient);

                await apiClient.GetProcessesAsync();
            }
            toolRunner.WarnPrivateKey -= ToolRunner_WarnPrivateKey;

            Assert.Single(_warnPrivateKeyLog);
        }


        /// <summary>
        /// Tests that --temp-apikey flag can be used to generate a key.
        /// </summary>
        [Fact]
        public async Task TempApiKeyTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;
            toolRunner.UseTempApiKey = true;

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Assert that the httpClient came back with a populated Authorization header
            Assert.NotNull(httpClient.DefaultRequestHeaders.Authorization?.Parameter);

            // Check that /processes is authenticated
            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);

            // Test that clearing the Authorization header will result in a 401
            httpClient.DefaultRequestHeaders.Authorization = null;
            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        /// <summary>
        /// Tests that --temp-apikey will override the ApiAuthentication configuration.
        /// </summary>
        [Fact]
        public async Task TempApiKeyOverridesApiAuthenticationTest()
        {
            const string signingAlgo = "ES256";
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.UseTempApiKey = true;

            // Set API key via key-per-file
            RootOptions options = new();
            options.UseApiKey(signingAlgo, Guid.NewGuid(), out string apiKey);
            toolRunner.WriteKeyPerValueConfiguration(options);

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /processes is authenticated when using the supplied temp key
            var processes = await apiClient.GetProcessesAsync();
            Assert.NotNull(processes);

            // Test that setting the Authorization header for the supplied config will result in a 401
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, apiKey);
            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);
        }

        /// <summary>
        /// Tests that Negotiate authentication can be used for authentication.
        /// </summary>
        [ConditionalFact(typeof(TestConditions), nameof(TestConditions.IsWindows))]
        public async Task NegotiateAuthenticationSchemeTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            await toolRunner.StartAsync();

            // Create HttpClient and HttpClientHandler that uses the current
            // user's credentials from the test process. Since dotnet-monitor
            // is launched by the test process, the usage of these credentials
            // should authenticate correctly (except when elevated, which the
            // tool will deny authorization).
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, ServiceProviderFixture.HttpClientName_DefaultCredentials);
            ApiClient client = new(_outputHelper, httpClient);

            // TODO: Split test into elevated vs non-elevated tests and skip
            // when not running in the corresponding context. Possibly unelevate
            // dotnet-monitor when running tests elevated.
            if (EnvironmentInformation.IsElevated)
            {
                var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                    () => client.GetProcessesAsync());
                Assert.Equal(HttpStatusCode.Forbidden, statusCodeException.StatusCode);
            }
            else
            {
                // Check that /processes does not challenge for authentication
                var processes = await client.GetProcessesAsync();
                Assert.NotNull(processes);
            }
        }

        private void ToolRunner_WarnPrivateKey(string fieldName)
        {
            _warnPrivateKeyLog.Add((fieldName, DateTime.Now));
        }

        private static JwtPayload GetJwtPayload(string audience, string subject, string issuer)
        {
            List<Claim> claims = new();

            if (audience != null)
            {
                Claim audClaim = new Claim(AuthConstants.ClaimAudienceStr, audience);
                claims.Add(audClaim);
            }
            if (subject != null)
            {
                Claim audClaim = new Claim(AuthConstants.ClaimSubjectStr, subject);
                claims.Add(audClaim);
            }
            if (issuer != null)
            {
                Claim audClaim = new Claim(AuthConstants.ClaimIssuerStr, issuer);
                claims.Add(audClaim);
            }

            JwtPayload newPayload = new JwtPayload(claims);

            return newPayload;
        }
    }
}
