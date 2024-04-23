// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal sealed class ApiKeyToken
    {
        public static string Create(ApiKeySignInfo signInfo, JwtPayload customPayload)
        {
            JwtSecurityToken newToken = new JwtSecurityToken(signInfo.Header, customPayload);
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(newToken);
        }
    }
}
