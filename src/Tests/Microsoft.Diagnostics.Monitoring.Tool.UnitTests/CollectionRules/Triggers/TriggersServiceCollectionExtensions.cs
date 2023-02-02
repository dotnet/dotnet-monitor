// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers
{
    internal static class TriggersServiceCollectionExtensions
    {
        public static IServiceCollection RegisterManualTrigger(this IServiceCollection services, ManualTriggerService service)
        {
            services.AddSingleton(service);
            return services.RegisterCollectionRuleTrigger<ManualTriggerFactory>(ManualTrigger.TriggerName);
        }
    }
}
