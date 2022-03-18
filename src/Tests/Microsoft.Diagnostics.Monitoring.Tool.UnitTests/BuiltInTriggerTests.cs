// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.Pipelines;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers.EventPipeTriggerFactory;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class BuiltInTriggerTests
    {
        private const string DefaultRuleName = nameof(BuiltInTriggerTests);
        private readonly TimeSpan SlidingWindowDurationDefault = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);
        private const string SystemRuntime = "System.Runtime";

        private ITestOutputHelper _outputHelper;

        public BuiltInTriggerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task GCHeapSizeTrigger_CorrectCustomOptions()
        {
            const double ExpectedLessThan = 20;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.Parse("00:00:03");

            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "gc-heap-size",
                GreaterThan = null,
                LessThan = ExpectedLessThan,
                SlidingWindowDuration = ExpectedSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<GCHeapSizeOptions>(KnownCollectionRuleTriggers.GCHeapSize, expectedSettings, options =>
            {
                options.LessThan = ExpectedLessThan;
                options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task ThreadpoolQueueLengthTrigger_CorrectCustomOptions()
        {
            const double ExpectedLessThan = 20;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.Parse("00:00:03");

            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "threadpool-queue-length",
                GreaterThan = null,
                LessThan = ExpectedLessThan,
                SlidingWindowDuration = ExpectedSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<ThreadpoolQueueLengthOptions>(KnownCollectionRuleTriggers.ThreadpoolQueueLength, expectedSettings, options =>
            {
                options.LessThan = ExpectedLessThan;
                options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task HighCPUTrigger_CorrectCustomOptions()
        {
            const double ExpectedLessThan = 20;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.Parse("00:00:03");

            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "cpu-usage",
                GreaterThan = null,
                LessThan = ExpectedLessThan,
                SlidingWindowDuration = ExpectedSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<HighCPUOptions>(KnownCollectionRuleTriggers.HighCPU, expectedSettings, options =>
            {
                options.LessThan = ExpectedLessThan;
                options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task GCHeapSizeTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "gc-heap-size",
                GreaterThan = GCHeapSizeOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = SlidingWindowDurationDefault
            };

            await IEventCounterTrigger_CorrectOptions<GCHeapSizeOptions>(KnownCollectionRuleTriggers.GCHeapSize, expectedSettings);
        }

        [Fact]
        public async Task ThreadpoolQueueLengthTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "threadpool-queue-length",
                GreaterThan = ThreadpoolQueueLengthOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = SlidingWindowDurationDefault
            };

            await IEventCounterTrigger_CorrectOptions<ThreadpoolQueueLengthOptions>(KnownCollectionRuleTriggers.ThreadpoolQueueLength, expectedSettings);
        }

        [Fact]
        public async Task HighCPUTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = SystemRuntime,
                CounterName = "cpu-usage",
                GreaterThan = HighCPUOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = SlidingWindowDurationDefault
            };

            await IEventCounterTrigger_CorrectOptions<HighCPUOptions>(KnownCollectionRuleTriggers.HighCPU, expectedSettings);
        }

        private async Task IEventCounterTrigger_CorrectOptions<T>(string triggerName, EventCounterOptions expectedSettings, Action<IEventCounterShortcuts> callback = null)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetIEventCounterTrigger(typeof(T), triggerName, callback);
            }, host =>
            {
                T options = GetTriggerOptions<T>(host, DefaultRuleName);

                ICollectionRuleTriggerFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleTriggerOperations>().TryCreateFactory(triggerName, out factory));

                EventPipeTrigger<EventCounterTriggerSettings> trigger = (EventPipeTrigger<EventCounterTriggerSettings>)factory.Create(new EndpointInfo(), null, options);

                // We have to crack open EventPipeTrigger from private to internal (and _pipeline as well) to get this to work -> is that acceptable purely for the purposes of testing coverage with the built-in defaults?
                EventCounterTriggerSettings triggerSettings = trigger._pipeline.Settings.TriggerSettings;

                Assert.Equal(expectedSettings.CounterName, triggerSettings.CounterName);
                Assert.Equal(expectedSettings.ProviderName, triggerSettings.ProviderName);
                Assert.Equal(expectedSettings.GreaterThan, triggerSettings.GreaterThan);
                Assert.Equal(expectedSettings.LessThan, triggerSettings.LessThan);
                Assert.Equal(expectedSettings.SlidingWindowDuration, triggerSettings.SlidingWindowDuration);
            });
        }

        // This will be removed once CollectionRuleDefaults is merged in
        internal static T GetTriggerOptions<T>(IHost host, string ruleName)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Trigger.Settings;

            return options;
        }
    }
}
