// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static partial class CollectionRuleOptionsExtensions
    {
        public static CollectionRuleOptions AddCommandLineFilter(this CollectionRuleOptions options, string value, ProcessFilterType matchType = ProcessFilterType.Contains)
        {
            options.Filters.Add(new ProcessFilterDescriptor()
            {
                Key = ProcessFilterKey.CommandLine,
                Value = value,
                MatchType = matchType
            });

            return options;
        }

        public static CollectionRuleOptions AddProcessNameFilter(this CollectionRuleOptions options, string name)
        {
            options.Filters.Add(new ProcessFilterDescriptor()
            {
                Key = ProcessFilterKey.ProcessName,
                Value = name,
                MatchType = ProcessFilterType.Exact
            });

            return options;
        }

        public static CollectionRuleOptions AddAction(this CollectionRuleOptions options, string type, Action<CollectionRuleActionOptions> callback = null)
        {
            CollectionRuleActionOptions actionOptions = new();
            actionOptions.Type = type;

            callback?.Invoke(actionOptions);

            options.Actions.Add(actionOptions);

            return options;
        }

        public static CollectionRuleOptions AddCollectDumpAction(this CollectionRuleOptions options, string egress = null, Action<CollectDumpOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectDump,
                actionOptions =>
                {
                    CollectDumpOptions collectDumpOptions = new() { Egress = egress };
                    callback?.Invoke(collectDumpOptions);

                    actionOptions.Settings = collectDumpOptions;
                });
        }

        public static CollectionRuleOptions AddCollectGCDumpAction(this CollectionRuleOptions options, string egress, Action<CollectGCDumpOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectGCDump,
                actionOptions =>
                {
                    CollectGCDumpOptions collectGCDumpOptions = new() { Egress = egress };
                    callback?.Invoke(collectGCDumpOptions);

                    actionOptions.Settings = collectGCDumpOptions;
                });
        }

        public static CollectionRuleOptions AddCollectLogsAction(this CollectionRuleOptions options, string egress, Action<CollectLogsOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectLogs,
                actionOptions =>
                {
                    CollectLogsOptions collectLogsOptions = new() { Egress = egress };
                    callback?.Invoke(collectLogsOptions);

                    actionOptions.Settings = collectLogsOptions;
                });
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, TraceProfile profile, string egress, Action<CollectTraceOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectTrace,
                actionOptions =>
                {
                    CollectTraceOptions collectTraceOptions = new()
                    {
                        Egress = egress,
                        Profile = profile,
                    };

                    callback?.Invoke(collectTraceOptions);

                    actionOptions.Settings = collectTraceOptions;
                });
        }

        public static CollectionRuleOptions AddCollectTraceAction(this CollectionRuleOptions options, IEnumerable<EventPipeProvider> providers, string egress, Action<CollectTraceOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectTrace,
                actionOptions =>
                {
                    CollectTraceOptions collectTraceOptions = new()
                    {
                        Egress = egress,
                        Providers = new List<EventPipeProvider>(providers),
                    };

                    callback?.Invoke(collectTraceOptions);

                    actionOptions.Settings = collectTraceOptions;
                });
        }

        public static CollectionRuleOptions AddCollectLiveMetricsAction(this CollectionRuleOptions options, string egress = null, Action<CollectLiveMetricsOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectLiveMetrics,
                actionOptions =>
                {
                    CollectLiveMetricsOptions collectLiveMetricsOptions = new() { Egress = egress };
                    callback?.Invoke(collectLiveMetricsOptions);

                    actionOptions.Settings = collectLiveMetricsOptions;
                });
        }

        public static CollectionRuleOptions AddCollectStacksAction(this CollectionRuleOptions options, string egress, Action<CollectStacksOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectStacks,
                actionOptions =>
                {
                    CollectStacksOptions collectStacksOptions = new() { Egress = egress };
                    callback?.Invoke(collectStacksOptions);

                    actionOptions.Settings = collectStacksOptions;
                });
        }

        public static CollectionRuleOptions AddExecuteAction(this CollectionRuleOptions options, string path, string arguments = null, bool? waitForCompletion = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.Execute,
                actionOptions =>
                {
                    ExecuteOptions executeOptions = new();
                    executeOptions.Arguments = arguments;
                    executeOptions.Path = path;

                    actionOptions.Settings = executeOptions;
                    actionOptions.WaitForCompletion = waitForCompletion;
                });
        }

        public static CollectionRuleOptions AddExecuteActionAppAction(this CollectionRuleOptions options, Assembly testAssembly, params string[] args)
        {
            options.AddExecuteAction(TestDotNetHost.GetPath(), ExecuteActionTestHelper.GenerateArgumentsString(testAssembly, args));

            return options;
        }

        public static CollectionRuleOptions AddExecuteActionAppAction(this CollectionRuleOptions options, Assembly testAssembly, bool waitForCompletion, params string[] args)
        {
            options.AddExecuteAction(TestDotNetHost.GetPath(), ExecuteActionTestHelper.GenerateArgumentsString(testAssembly, args), waitForCompletion);

            return options;
        }

        public static CollectionRuleOptions AddLoadProfilerAction(this CollectionRuleOptions options, Action<LoadProfilerOptions> callback = null)
        {
            return options.AddAction(
                 KnownCollectionRuleActions.LoadProfiler,
                 callback: actionOptions =>
                 {
                     LoadProfilerOptions loadProfilerOptions = new();
                     callback?.Invoke(loadProfilerOptions);
                     actionOptions.Settings = loadProfilerOptions;
                 });
        }

        public static CollectionRuleOptions AddSetEnvironmentVariableAction(this CollectionRuleOptions options, string name, string value = null)
        {
            return options.AddAction(
                 KnownCollectionRuleActions.SetEnvironmentVariable,
                 callback: actionOptions =>
                 {
                     SetEnvironmentVariableOptions setEnvOpts = new()
                     {
                         Name = name,
                         Value = value,
                     };
                     actionOptions.Settings = setEnvOpts;
                 });
        }

        public static CollectionRuleOptions AddGetEnvironmentVariableAction(this CollectionRuleOptions options, string name)
        {
            return options.AddAction(
                 KnownCollectionRuleActions.GetEnvironmentVariable,
                 callback: actionOptions =>
                 {
                     GetEnvironmentVariableOptions getEnvOpts = new()
                     {
                         Name = name,
                     };
                     actionOptions.Settings = getEnvOpts;
                 });
        }

        public static CollectionRuleOptions AddCollectExceptionsAction(this CollectionRuleOptions options, string egress, Action<CollectExceptionsOptions> callback = null)
        {
            return options.AddAction(
                KnownCollectionRuleActions.CollectExceptions,
                actionOptions =>
                {
                    CollectExceptionsOptions collectExceptionsOptions = new() { Egress = egress };
                    callback?.Invoke(collectExceptionsOptions);

                    actionOptions.Settings = collectExceptionsOptions;
                });
        }

        public static CollectionRuleOptions SetActionLimits(this CollectionRuleOptions options, int? count = null, TimeSpan? slidingWindowDuration = null, TimeSpan? ruleDuration = null)
        {
            if (null == options.Limits)
            {
                options.Limits = new CollectionRuleLimitsOptions();
            }

            options.Limits.ActionCount = count;
            options.Limits.ActionCountSlidingWindowDuration = slidingWindowDuration;
            options.Limits.RuleDuration = ruleDuration;

            return options;
        }

        public static CollectionRuleOptions SetDurationLimit(this CollectionRuleOptions options, TimeSpan duration)
        {
            if (null == options.Limits)
            {
                options.Limits = new CollectionRuleLimitsOptions();
            }

            options.Limits.RuleDuration = duration;

            return options;
        }

        public static CollectionRuleOptions SetCPUUsageTrigger(this CollectionRuleOptions options, Action<CPUUsageOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.CPUUsage,
                triggerOptions =>
                {
                    CPUUsageOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetThreadpoolQueueLengthTrigger(this CollectionRuleOptions options, Action<ThreadpoolQueueLengthOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.ThreadpoolQueueLength,
                triggerOptions =>
                {
                    ThreadpoolQueueLengthOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetGCHeapSizeTrigger(this CollectionRuleOptions options, Action<GCHeapSizeOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.GCHeapSize,
                triggerOptions =>
                {
                    GCHeapSizeOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetIEventCounterTrigger(this CollectionRuleOptions options, Type triggerType, string triggerName, Action<IEventCounterShortcuts> callback = null)
        {
            return options.SetTrigger(
                triggerName,
                triggerOptions =>
                {
                    var settings = Activator.CreateInstance(triggerType);

                    callback?.Invoke((IEventCounterShortcuts)settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetEventCounterTrigger(this CollectionRuleOptions options, Action<EventCounterOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.EventCounter,
                triggerOptions =>
                {
                    EventCounterOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetEventMeterTrigger(this CollectionRuleOptions options, Action<EventMeterOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.EventMeter,
                triggerOptions =>
                {
                    EventMeterOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetAspNetRequestCountTrigger(this CollectionRuleOptions options, Action<AspNetRequestCountOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.AspNetRequestCount,
                triggerOptions =>
                {
                    AspNetRequestCountOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetAspNetRequestDurationTrigger(this CollectionRuleOptions options, Action<AspNetRequestDurationOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.AspNetRequestDuration,
                triggerOptions =>
                {
                    AspNetRequestDurationOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetAspNetResponseStatusTrigger(this CollectionRuleOptions options, Action<AspNetResponseStatusOptions> callback = null)
        {
            return options.SetTrigger(
                KnownCollectionRuleTriggers.AspNetResponseStatus,
                triggerOptions =>
                {
                    AspNetResponseStatusOptions settings = new();

                    callback?.Invoke(settings);

                    triggerOptions.Settings = settings;
                });
        }

        public static CollectionRuleOptions SetStartupTrigger(this CollectionRuleOptions options)
        {
            return SetTrigger(options, KnownCollectionRuleTriggers.Startup);
        }

        public static CollectionRuleOptions SetTrigger(this CollectionRuleOptions options, string type, Action<CollectionRuleTriggerOptions> callback = null)
        {
            CollectionRuleTriggerOptions triggerOptions = new();
            triggerOptions.Type = type;

            callback?.Invoke(triggerOptions);

            options.Trigger = triggerOptions;

            return options;
        }

        public static CollectionRuleOptions SetLimits(this CollectionRuleOptions options, Action<CollectionRuleLimitsOptions> callback = null)
        {
            CollectionRuleLimitsOptions limitsOptions = new();

            callback?.Invoke(limitsOptions);

            options.Limits = limitsOptions;

            return options;
        }

        public static EventCounterOptions VerifyEventCounterTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.EventCounter);
            return Assert.IsType<EventCounterOptions>(ruleOptions.Trigger.Settings);
        }

        public static EventMeterOptions VerifyEventMeterTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.EventMeter);
            return Assert.IsType<EventMeterOptions>(ruleOptions.Trigger.Settings);
        }

        public static CPUUsageOptions VerifyCPUUsageTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.CPUUsage);
            return Assert.IsType<CPUUsageOptions>(ruleOptions.Trigger.Settings);
        }

        public static GCHeapSizeOptions VerifyGCHeapSizeTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.GCHeapSize);
            return Assert.IsType<GCHeapSizeOptions>(ruleOptions.Trigger.Settings);
        }

        public static ThreadpoolQueueLengthOptions VerifyThreadpoolQueueLengthTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.ThreadpoolQueueLength);
            return Assert.IsType<ThreadpoolQueueLengthOptions>(ruleOptions.Trigger.Settings);
        }

        public static IEventCounterShortcuts VerifyIEventCounterTrigger(this CollectionRuleOptions ruleOptions, Type triggerType, string triggerName)
        {
            ruleOptions.VerifyTrigger(triggerName);

            Assert.IsType(triggerType, ruleOptions.Trigger.Settings);

            return (IEventCounterShortcuts)ruleOptions.Trigger.Settings;
        }

        public static AspNetRequestCountOptions VerifyAspNetRequestCountTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.AspNetRequestCount);
            return Assert.IsType<AspNetRequestCountOptions>(ruleOptions.Trigger.Settings);
        }

        public static AspNetRequestDurationOptions VerifyAspNetRequestDurationTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.AspNetRequestDuration);
            return Assert.IsType<AspNetRequestDurationOptions>(ruleOptions.Trigger.Settings);
        }

        public static AspNetResponseStatusOptions VerifyAspNetResponseStatusTrigger(this CollectionRuleOptions ruleOptions)
        {
            ruleOptions.VerifyTrigger(KnownCollectionRuleTriggers.AspNetResponseStatus);
            return Assert.IsType<AspNetResponseStatusOptions>(ruleOptions.Trigger.Settings);
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

        public static CollectTraceOptions VerifyCollectTraceAction(this CollectionRuleOptions ruleOptions, int actionIndex, IEnumerable<EventPipeProvider> providers, string expectedEgress, TraceEventFilter expectedStoppingEvent = null)
        {
            CollectTraceOptions collectTraceOptions = ruleOptions.VerifyAction<CollectTraceOptions>(
                actionIndex, KnownCollectionRuleActions.CollectTrace);

            Assert.Equal(expectedEgress, collectTraceOptions.Egress);
            Assert.NotNull(collectTraceOptions.Providers);
            Assert.Equal(providers.Count(), collectTraceOptions.Providers.Count);
            Assert.Equal(expectedStoppingEvent, collectTraceOptions.StoppingEvent);

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

        public static CollectLiveMetricsOptions VerifyCollectLiveMetricsAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedEgress)
        {
            CollectLiveMetricsOptions collectLiveMetricsOptions = ruleOptions.VerifyAction<CollectLiveMetricsOptions>(
                actionIndex, KnownCollectionRuleActions.CollectLiveMetrics);

            Assert.Equal(expectedEgress, collectLiveMetricsOptions.Egress);

            return collectLiveMetricsOptions;
        }

        public static ExecuteOptions VerifyExecuteAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedPath, string expectedArguments = null)
        {
            ExecuteOptions executeOptions = ruleOptions.VerifyAction<ExecuteOptions>(
                actionIndex, KnownCollectionRuleActions.Execute);

            Assert.Equal(expectedPath, executeOptions.Path);
            Assert.Equal(expectedArguments, executeOptions.Arguments);

            return executeOptions;
        }

        public static LoadProfilerOptions VerifyLoadProfilerAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedPath, Guid expectedClsid)
        {
            LoadProfilerOptions opts = ruleOptions.VerifyAction<LoadProfilerOptions>(
                actionIndex, KnownCollectionRuleActions.LoadProfiler);

            Assert.Equal(expectedPath, opts.Path);
            Assert.Equal(expectedClsid, opts.Clsid);

            return opts;
        }

        public static SetEnvironmentVariableOptions VerifySetEnvironmentVariableAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedName, string expectedValue)
        {
            SetEnvironmentVariableOptions opts = ruleOptions.VerifyAction<SetEnvironmentVariableOptions>(
                actionIndex, KnownCollectionRuleActions.SetEnvironmentVariable);

            Assert.Equal(expectedName, opts.Name);
            Assert.Equal(expectedValue, opts.Value);

            return opts;
        }
        public static GetEnvironmentVariableOptions VerifyGetEnvironmentVariableAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedName)
        {
            GetEnvironmentVariableOptions opts = ruleOptions.VerifyAction<GetEnvironmentVariableOptions>(
                actionIndex, KnownCollectionRuleActions.GetEnvironmentVariable);

            Assert.Equal(expectedName, opts.Name);

            return opts;
        }

        public static CollectExceptionsOptions VerifyCollectExceptionsAction(this CollectionRuleOptions ruleOptions, int actionIndex, string expectedEgress)
        {
            CollectExceptionsOptions collectExceptionsOptions = ruleOptions.VerifyAction<CollectExceptionsOptions>(
                actionIndex, KnownCollectionRuleActions.CollectExceptions);

            Assert.Equal(expectedEgress, collectExceptionsOptions.Egress);

            return collectExceptionsOptions;
        }

        private static TOptions VerifyAction<TOptions>(this CollectionRuleOptions ruleOptions, int actionIndex, string actionType)
        {
            CollectionRuleActionOptions actionOptions = ruleOptions.Actions[actionIndex];

            Assert.Equal(actionType, actionOptions.Type);

            return Assert.IsType<TOptions>(actionOptions.Settings);
        }
    }
}
