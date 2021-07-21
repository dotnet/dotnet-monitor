﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class OperationExtensions
    {
        public static IServiceCollection ConfigureOperationStore(this IServiceCollection services)
        {
            services.AddSingleton<EgressOperationQueue>();
            services.AddSingleton<EgressOperationStore>();
            services.AddHostedService<EgressOperationService>();
            return services;
        }
    }
}
