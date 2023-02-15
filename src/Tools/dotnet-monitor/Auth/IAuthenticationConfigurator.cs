// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth
{
    internal interface IAuthenticationConfigurator
    {
        void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context);
        void ConfigureSwaggerGenAuth(SwaggerGenOptions options);
        IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider serviceProvider);
    }
}
