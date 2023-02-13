// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Stored
{
    internal sealed class MonitorKeyAuthHandler : AbstractMonitorKeyAuthHandler
    {
        public MonitorKeyAuthHandler() : base()
        {
        }

        protected override void ConfigureAuthBuilder(IServiceCollection services, HostBuilderContext context, AuthenticationBuilder authBuilder)
        {
            services.ConfigureMonitorApiKeyAuthentication(context.Configuration, authBuilder);
            services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
            services.AddSingleton<MonitorApiKeyConfigurationObserver>();
        }

        public override void ConfigureSwaggerUI(SwaggerUIOptions options)
        {

        }

        public override void LogStartup(ILogger logger, IServiceProvider serviceProvider)
        {
            LogIfNegotiateIsDisabledDueToElevation(logger);

            MonitorApiKeyConfigurationObserver observer = serviceProvider.GetRequiredService<MonitorApiKeyConfigurationObserver>();
            observer.Initialize();
        }
    }
}
