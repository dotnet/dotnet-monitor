// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static class CollectionRuleOptionsExtensions
    {
        public static CollectionRuleOptions AddAction(this CollectionRuleOptions options, string type)
        {
            return options.AddAction(type, out _);
        }

        public static CollectionRuleOptions AddAction(this CollectionRuleOptions options, string type, out CollectionRuleActionOptions actionOptions)
        {
            actionOptions = new();
            actionOptions.Type = type;

            options.Actions.Add(actionOptions);

            return options;
        }

        public static CollectionRuleOptions AddCollectDumpAction(this CollectionRuleOptions options, DumpType type, string egress)
        {
            options.AddAction(KnownCollectionRuleActions.CollectDump, out CollectionRuleActionOptions actionOptions);

            CollectDumpOptions collectDumpOptions = new();
            collectDumpOptions.Egress = egress;
            collectDumpOptions.Type = type;

            actionOptions.Settings = collectDumpOptions;

            return options;
        }

        public static CollectionRuleOptions AddCollectGCDumpAction(this CollectionRuleOptions options, string egress)
        {
            options.AddAction(KnownCollectionRuleActions.CollectGCDump, out CollectionRuleActionOptions actionOptions);

            CollectGCDumpOptions collectGCDumpOptions = new();
            collectGCDumpOptions.Egress = egress;

            actionOptions.Settings = collectGCDumpOptions;

            return options;
        }

        public static CollectionRuleOptions AddCollectLogsAction(this CollectionRuleOptions options, string egress)
        {
            return options.AddCollectLogsAction(egress, out _);
        }

        public static CollectionRuleOptions AddCollectLogsAction(this CollectionRuleOptions options, string egress, out CollectLogsOptions collectLogsOptions)
        {
            options.AddAction(KnownCollectionRuleActions.CollectLogs, out CollectionRuleActionOptions actionOptions);

            collectLogsOptions = new();
            collectLogsOptions.Egress = egress;

            actionOptions.Settings = collectLogsOptions;

            return options;
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, TraceProfile profile, string egress)
        {
            return options.AddCollectTraceAction(profile, egress, out _);
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, TraceProfile profile, string egress, out CollectTraceOptions collectTraceOptions)
        {
            options.AddAction(KnownCollectionRuleActions.CollectTrace, out CollectionRuleActionOptions actionOptions);

            collectTraceOptions = new();
            collectTraceOptions.Profile = profile;
            collectTraceOptions.Egress = egress;

            actionOptions.Settings = collectTraceOptions;

            return options;
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, IEnumerable<EventPipeProvider> providers, string egress)
        {
            return options.AddCollectTraceAction(providers, egress, out _);
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, IEnumerable<EventPipeProvider> providers, string egress, out CollectTraceOptions collectTraceOptions)
        {
            options.AddAction(KnownCollectionRuleActions.CollectTrace, out CollectionRuleActionOptions actionOptions);

            collectTraceOptions = new();
            collectTraceOptions.Providers = new List<EventPipeProvider>(providers);
            collectTraceOptions.Egress = egress;

            actionOptions.Settings = collectTraceOptions;

            return options;
        }

        public static CollectionRuleOptions AddExecuteAction(this CollectionRuleOptions options, string path, string arguments = null)
        {
            options.AddAction(KnownCollectionRuleActions.Execute, out CollectionRuleActionOptions actionOptions);

            ExecuteOptions executeOptions = new();
            executeOptions.Arguments = arguments;
            executeOptions.Path = path;

            actionOptions.Settings = executeOptions;

            return options;
        }

        public static CollectionRuleOptions AddExecuteActionAppAction(this CollectionRuleOptions options, params string[] args)
        {
            options.AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(args));

            return options;
        }

        public static CollectionRuleOptions SetEventCounterTrigger(this CollectionRuleOptions options, out EventCounterOptions settings)
        {
            SetTrigger(options, KnownCollectionRuleTriggers.EventCounter, out CollectionRuleTriggerOptions triggerOptions);

            settings = new();

            triggerOptions.Settings = settings;

            return options;
        }

        public static CollectionRuleOptions SetStartupTrigger(this CollectionRuleOptions options)
        {
            return SetTrigger(options, KnownCollectionRuleTriggers.Startup, out _);
        }

        public static CollectionRuleOptions SetTrigger(this CollectionRuleOptions options, string type)
        {
            return options.SetTrigger(type, out _);
        }

        public static CollectionRuleOptions SetTrigger(this CollectionRuleOptions options, string type, out CollectionRuleTriggerOptions triggerOptions)
        {
            triggerOptions = new();
            triggerOptions.Type = type;

            options.Trigger = triggerOptions;

            return options;
        }

        public static EventCounterOptions VerifyEventCounterTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.EventCounter);
            return Assert.IsType<EventCounterOptions>(ruleOptions.Trigger.Settings);
        }

        public static void VerifyStartupTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.Startup);
            Assert.Null(ruleOptions.Trigger.Settings);
        }

        private static void VerifyTrigger(this CollectionRuleOptions ruleOptions, string triggerType)
        {
            Assert.NotNull(ruleOptions.Trigger);
            Assert.Equal(triggerType, ruleOptions.Trigger.Type);
        }

        public static CollectDumpOptions VerifyCollectDumpAction(this CollectionRuleOptions ruleOptions, int actionIndex, DumpType expectedDumpType, string expectedEgress)
        {
            CollectDumpOptions collectDumpOptions = ruleOptions.VerifyAction<CollectDumpOptions>(
                actionIndex, KnownCollectionRuleActions.CollectDump);

            Assert.Equal(expectedDumpType, collectDumpOptions.Type);
            Assert.Equal(expectedEgress, collectDumpOptions.Egress);

            return collectDumpOptions;
        }

        public static CollectGCDumpOptions VerifyCollectGCDumpAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedEgress)
        {
            CollectGCDumpOptions collectGCDumpOptions = ruleOptions.VerifyAction<CollectGCDumpOptions>(
                actionIndex, KnownCollectionRuleActions.CollectGCDump);

            Assert.Equal(expectedEgress, collectGCDumpOptions.Egress);

            return collectGCDumpOptions;
        }

        public static CollectLogsOptions VerifyCollectLogsAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedEgress)
        {
            CollectLogsOptions collectLogsOptions = ruleOptions.VerifyAction<CollectLogsOptions>(
                actionIndex, KnownCollectionRuleActions.CollectLogs);

            Assert.Equal(expectedEgress, collectLogsOptions.Egress);

            return collectLogsOptions;
        }

        public static CollectTraceOptions VerifyCollectTraceAction(this CollectionRuleOptions ruleOptions, int actionIndex, TraceProfile expectedProfile, string expectedEgress)
        {
            CollectTraceOptions collectTraceOptions = ruleOptions.VerifyAction<CollectTraceOptions>(
                actionIndex, KnownCollectionRuleActions.CollectTrace);

            Assert.Equal(expectedProfile, collectTraceOptions.Profile);
            Assert.Equal(expectedEgress, collectTraceOptions.Egress);

            return collectTraceOptions;
        }

        public static CollectTraceOptions VerifyCollectTraceAction(this CollectionRuleOptions ruleOptions, int actionIndex, IEnumerable<EventPipeProvider> providers, string expectedEgress)
        {
            CollectTraceOptions collectTraceOptions = ruleOptions.VerifyAction<CollectTraceOptions>(
                actionIndex, KnownCollectionRuleActions.CollectTrace);

            Assert.Equal(expectedEgress, collectTraceOptions.Egress);
            Assert.NotNull(collectTraceOptions.Providers);
            Assert.Equal(providers.Count(), collectTraceOptions.Providers.Count);

            int index = 0;
            foreach (EventPipeProvider expectedProvider in providers)
            {
                EventPipeProvider actualProvider = collectTraceOptions.Providers[index];
                Assert.Equal(expectedProvider.Name, actualProvider.Name);
                Assert.Equal(expectedProvider.Keywords, actualProvider.Keywords);
                Assert.Equal(expectedProvider.EventLevel, actualProvider.EventLevel);
                if (null == expectedProvider.Arguments)
                {
                    Assert.Null(actualProvider.Arguments);
                }
                else
                {
                    Assert.NotNull(actualProvider.Arguments);
                    Assert.Equal(expectedProvider.Arguments.Count, actualProvider.Arguments.Count);
                    foreach ((string expectedKey, string expectedValue) in expectedProvider.Arguments)
                    {
                        Assert.True(actualProvider.Arguments.TryGetValue(expectedKey, out string actualValue));
                        Assert.Equal(expectedValue, actualValue);
                    }
                }

                index++;
            }

            return collectTraceOptions;
        }

        public static ExecuteOptions VerifyExecuteAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedPath, string expectedArguments = null)
        {
            ExecuteOptions executeOptions = ruleOptions.VerifyAction<ExecuteOptions>(
                actionIndex, KnownCollectionRuleActions.Execute);

            Assert.Equal(expectedPath, executeOptions.Path);
            Assert.Equal(expectedArguments, executeOptions.Arguments);

            return executeOptions;
        }

        private static TOptions VerifyAction<TOptions>(this CollectionRuleOptions ruleOptions, int actionIndex, string actionType)
        {
            CollectionRuleActionOptions actionOptions = ruleOptions.Actions[actionIndex];

            Assert.Equal(actionType, actionOptions.Type);

            return Assert.IsType<TOptions>(actionOptions.Settings);
        }
    }
}
