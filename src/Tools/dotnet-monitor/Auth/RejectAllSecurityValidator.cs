
/* Unmerged change from project 'dotnet-monitor(net6.0)'
Before:
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
After:
using Microsoft.IdentityModel.Tokens;
*/
using Microsoft.IdentityModel.Tokens;
using 
/* Unmerged change from project 'dotnet-monitor(net6.0)'
Before:
using System.Collections.Generic;
using System.Security.Claims;
After:
using System.Security.Claims;
*/
System;
using System.Security.Claims;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// This will reject all validations and is used when configuration is invalid and
    /// all attempts to authenticate should be rejected.
    /// </summary>
    internal sealed class RejectAllSecurityValidator : ISecurityTokenValidator
    {
        public bool CanValidateToken => true;

        public int MaximumTokenSizeInBytes
        {
            // We need to provide a maximum token size, so we pick the same as the default used by everything derived from TokenHandler
            get => TokenValidationParameters.DefaultMaximumTokenSizeInBytes;
            set => throw new NotImplementedException();
        }

        public bool CanReadToken(string securityToken) => true;

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            validatedToken = null;
            throw new InvalidOperationException(Strings.ErrorMessage_ApiKeyNotConfigured);
        }
    }
}
