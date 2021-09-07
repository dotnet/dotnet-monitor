// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions
{
    internal static class ActionsServiceCollectionExtensions
    {
        public static IServiceCollection RegisterTestAction(this IServiceCollection services, CallbackActionService callback)
        {
            services.AddSingleton(callback);
            services.RegisterCollectionRuleAction<CallbackAction, object>(CallbackAction.ActionName);
            return services;
        }
    }
}
