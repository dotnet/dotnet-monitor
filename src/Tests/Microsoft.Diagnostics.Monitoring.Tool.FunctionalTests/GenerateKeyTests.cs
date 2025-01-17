// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class GenerateKeyTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public GenerateKeyTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(null)]
        [InlineData(OutputFormat.Json)]
        [InlineData(OutputFormat.Text)]
        [InlineData(OutputFormat.Cmd)]
        [InlineData(OutputFormat.PowerShell)]
        [InlineData(OutputFormat.Shell)]
        [InlineData(OutputFormat.MachineJson)]
        public async Task GenerateKey(OutputFormat? format)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TestTimeouts.OperationTimeout);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await using MonitorGenerateKeyRunner toolRunner = new(_outputHelper);
            toolRunner.Format = format;
            await toolRunner.StartAsync(cancellationToken);
            await toolRunner.WaitForExitAsync(cancellationToken);

            string tokenStr = await toolRunner.GetBearerToken(cancellationToken);
            Assert.NotNull(tokenStr);

            // MachineJson doesn't have a format string header
            if (format != OutputFormat.MachineJson)
            {
                string formatStr = await toolRunner.GetFormat(cancellationToken);
                Assert.NotNull(formatStr);
                Assert.Equal(toolRunner.FormatUsed.ToString(), formatStr);
            }

            string subject = await toolRunner.GetSubject(cancellationToken);
            Assert.NotNull(subject);

            string publicKey = await toolRunner.GetPublicKey(cancellationToken);
            Assert.NotNull(publicKey);
            string pubKeyJson = Base64UrlEncoder.Decode(publicKey);
            Assert.NotNull(pubKeyJson);
            JsonWebKey validatingKey = JsonWebKey.Create(pubKeyJson);
            Assert.NotNull(validatingKey);

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            Assert.True(tokenHandler.CanReadToken(tokenStr));

            TokenValidationParameters tokenValidationParams = new TokenValidationParameters()
            {
                // Signing Settings
                RequireSignedTokens = true,
                ValidAlgorithms = JwtAlgorithmChecker.GetAllowedJwsAlgorithmList(),

                // Issuer Settings
                ValidateIssuer = true,
                ValidIssuer = AuthConstants.ApiKeyJwtInternalIssuer,

                // Issuer Signing Key Settings
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = new SecurityKey[] { validatingKey },
                TryAllIssuerSigningKeys = true,

                // Audience Settings
                ValidateAudience = true,
                ValidAudiences = new string[] { AuthConstants.ApiKeyJwtAudience },

                // Other Settings
                ValidateActor = false,
                ValidateLifetime = true,
            };
            // Required for CodeQL. 
            tokenValidationParams.EnableAadSigningKeyIssuerValidation();

            ClaimsPrincipal claimsPrinciple = tokenHandler.ValidateToken(tokenStr, tokenValidationParams, out SecurityToken validatedToken);

            Assert.True(claimsPrinciple.HasClaim(ClaimTypes.NameIdentifier, subject));
        }
    }
}
