// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Temporary
{
    internal sealed class MonitorTempKeyAuthConfigurator : AbstractMonitorKeyAuthConfigurator
    {
        private readonly GeneratedJwtKey _jwtKey;

        public MonitorTempKeyAuthConfigurator(GeneratedJwtKey jwtKey) : base()
        {
            _jwtKey = jwtKey;
        }

        protected override void ConfigureAuthBuilder(IServiceCollection services, HostBuilderContext context, AuthenticationBuilder authBuilder)
        {
            services.ConfigureMonitorApiKeyAuthentication(context.Configuration, authBuilder, allowConfigurationUpdates: false);
            services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
        }

        public override IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(() =>
            {
                LogIfNegotiateIsDisabledDueToElevation(logger);
                logger.LogTempKey(_jwtKey.Token);
            });
        }
    }
}
