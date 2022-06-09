// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions
{
    internal static class ActionsServiceCollectionExtensions
    {
        public static IServiceCollection RegisterTestAction(this IServiceCollection services, CallbackActionService callback)
        {
            services.AddSingleton(callback);
            services.RegisterCollectionRuleAction<CallbackActionFactory, BaseRecordOptions>(CallbackAction.ActionName);

            return services;
        }

        public static IServiceCollection RegisterDelayedTestAction(this IServiceCollection services, CallbackActionService callback)
        {
            services.AddSingleton(callback);
            services.RegisterCollectionRuleAction<DelayedCallbackActionFactory, BaseRecordOptions>(DelayedCallbackAction.ActionName);

            return services;
        }

        public static CollectionRuleOptions AddPassThroughAction(this CollectionRuleOptions options, string name,
            string input1, string input2, string input3)
        {
            return options.AddAction(
                nameof(PassThroughAction),
                actionOptions =>
                {
                    PassThroughOptions settings = new PassThroughOptions()
                    {
                        Input1 = input1,
                        Input2 = input2,
                        Input3 = input3
                    };
                    actionOptions.Name = name;
                    actionOptions.Settings = settings;
                });
        }
    }
}
