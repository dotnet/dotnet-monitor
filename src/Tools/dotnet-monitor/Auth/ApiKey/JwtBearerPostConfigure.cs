﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Configures <see cref="JwtBearerOptions"/> based on <see cref="MonitorApiKeyConfiguration" /> configuration.
    /// </summary>
    internal sealed class JwtBearerPostConfigure :
        IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IOptionsMonitor<MonitorApiKeyConfiguration> _apiKeyConfig;

        public JwtBearerPostConfigure(
            IOptionsMonitor<MonitorApiKeyConfiguration> apiKeyConfig)
        {
            _apiKeyConfig = apiKeyConfig;
        }

        public void PostConfigure(string name, JwtBearerOptions options)
        {
            MonitorApiKeyConfiguration configSnapshot = _apiKeyConfig.CurrentValue;
            if (!configSnapshot.Configured || configSnapshot.ValidationErrors.Any())
            {
                options.SecurityTokenValidators.Add(new RejectAllSecurityValidator());
                return;
            }

            options.ConfigureApiKeyTokenValidation(configSnapshot.PublicKey, configSnapshot.Issuer);
        }
    }
}
