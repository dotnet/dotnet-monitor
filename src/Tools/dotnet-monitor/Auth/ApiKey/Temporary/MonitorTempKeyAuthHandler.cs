﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Temporary
{
    internal sealed class MonitorTempKeyAuthHandler : AbstractMonitorKeyAuthHandler
    {
        private readonly GeneratedJwtKey _jwtKey;

        public MonitorTempKeyAuthHandler() : base()
        {
            _jwtKey = GeneratedJwtKey.Create();
        }

        protected override void ConfigureAuthBuilder(IServiceCollection services, HostBuilderContext context, AuthenticationBuilder authBuilder)
        {
            authBuilder.AddScheme<JwtBearerOptions, JwtBearerHandler>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Error handling:
                // If there's an exception here using the generated key
                // all routes will be inaccessible and the exceptions will be logged.
                string jwkJson = Base64UrlEncoder.Decode(_jwtKey.PublicKey);
                JsonWebKey jwk = JsonWebKey.Create(jwkJson);

                options.ConfigureApiKeyTokenValidation(jwk);
            });

            services.AddSingleton<IAuthorizationHandler>(new UserAuthorizationHandler(_jwtKey.Subject));
        }

        public override void ConfigureSwaggerUI(SwaggerUIOptions options)
        {

        }

        public override void LogStartup(ILogger logger, IServiceProvider serviceProvider)
        {
            LogIfNegotiateIsDisabledDueToElevation(logger);
            logger.LogTempKey(_jwtKey.Token);
        }
    }
}
