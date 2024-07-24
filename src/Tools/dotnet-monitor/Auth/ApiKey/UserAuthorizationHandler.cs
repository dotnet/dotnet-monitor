// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Handles authorization for both Negotiate and ApiKey authentication.
    /// </summary>
    internal sealed class UserAuthorizationHandler : AuthorizationHandler<AuthorizedUserRequirement>
    {
        private readonly IOptionsMonitor<MonitorApiKeyConfiguration> _apiKeyConfig;

        public UserAuthorizationHandler(IOptionsMonitor<MonitorApiKeyConfiguration> apiKeyConfig)
        {
            _apiKeyConfig = apiKeyConfig;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizedUserRequirement requirement)
        {
            if (context.User?.Identity == null)
            {
                return Task.CompletedTask;
            }

            if (context.User.Identity.AuthenticationType == AuthConstants.FederationAuthType)
            {
                // If we get a FederationAuthType (Bearer from a Jwt Token) we need to check that the user has the specified subject claim.
                MonitorApiKeyConfiguration configSnapshot = _apiKeyConfig.CurrentValue;
#nullable disable
                if (context.User.HasClaim(ClaimTypes.NameIdentifier, configSnapshot.Subject))
                {
                    context.Succeed(requirement);
                }
#nullable restore
            }
            else if ((context.User.Identity.AuthenticationType == AuthConstants.NtlmSchema) ||
                    (context.User.Identity.AuthenticationType == AuthConstants.KerberosSchema) ||
                    (context.User.Identity.AuthenticationType == AuthConstants.NegotiateSchema))
            {
                // Only supported on Windows
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Task.CompletedTask;
                }

                //Negotiate, Kerberos, or NTLM.
                //CONSIDER In the future, we may want to have configuration around a dotnet-monitor group sid instead.
                //We cannot check the user against BUILTIN\Administrators group membership, since the browser user
                //has a deny claim on Administrator group.
                //Validate that the user that logged in matches the user that is running dotnet-monitor
                //Do not allow at all if running as Administrator.
                if (EnvironmentInformation.IsElevated)
                {
                    return Task.CompletedTask;
                }

                using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                Claim? currentUserClaim = currentUser.Claims.FirstOrDefault(claim => string.Equals(claim.Type, ClaimTypes.PrimarySid));
                if ((currentUserClaim != null) && context.User.HasClaim(currentUserClaim.Type, currentUserClaim.Value))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
