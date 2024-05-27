// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class OperationExtensions
    {
        public static IServiceCollection ConfigureOperationStore(this IServiceCollection services)
        {
            services.AddSingleton<IEgressOperationQueue, EgressOperationQueue>();
            services.AddSingleton<IEgressOperationStore, EgressOperationStore>();
            services.AddHostedService<EgressOperationService>();
            return services;
        }
    }
}
