// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Stored
{
    internal sealed class MonitorKeyAuthConfigurator : AbstractMonitorKeyAuthConfigurator
    {
        public MonitorKeyAuthConfigurator() : base()
        {
        }

        protected override void ConfigureAuthBuilder(IServiceCollection services, HostBuilderContext context, AuthenticationBuilder authBuilder)
        {
            services.ConfigureMonitorApiKeyAuthentication(context.Configuration, authBuilder, allowConfigurationUpdates: true);
            services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
            services.AddSingleton<MonitorApiKeyConfigurationObserver>();
        }

        public override IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider serviceProvider)
        {
            return new AuthenticationStartupLoggerWrapper(() =>
            {
                LogIfNegotiateIsDisabledDueToElevation(logger);

                MonitorApiKeyConfigurationObserver observer = serviceProvider.GetRequiredService<MonitorApiKeyConfigurationObserver>();
                observer.Initialize();
            });
        }
    }
}
