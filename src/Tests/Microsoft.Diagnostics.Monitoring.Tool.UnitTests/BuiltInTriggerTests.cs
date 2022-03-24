// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
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
        private readonly TimeSpan CustomSlidingWindowDuration = TimeSpan.Parse("00:00:03");
        private const double CustomLessThan = 20;

        private ITestOutputHelper _outputHelper;

        public BuiltInTriggerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task GCHeapSizeTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.GCHeapSize,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<GCHeapSizeOptions>(KnownCollectionRuleTriggers.GCHeapSize, expectedSettings, options =>
            {
                options.LessThan = CustomLessThan;
                options.SlidingWindowDuration = CustomSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task ThreadpoolQueueLengthTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.ThreadpoolQueueLength,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<ThreadpoolQueueLengthOptions>(KnownCollectionRuleTriggers.ThreadpoolQueueLength, expectedSettings, options =>
            {
                options.LessThan = CustomLessThan;
                options.SlidingWindowDuration = CustomSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task HighCPUTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.CpuUsage,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            await IEventCounterTrigger_CorrectOptions<HighCPUOptions>(KnownCollectionRuleTriggers.HighCPU, expectedSettings, options =>
            {
                options.LessThan = CustomLessThan;
                options.SlidingWindowDuration = CustomSlidingWindowDuration;
            });
        }

        [Fact]
        public async Task GCHeapSizeTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.GCHeapSize,
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
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.ThreadpoolQueueLength,
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
                ProviderName = IEventCounterShortcutsConstants.SystemRuntime,
                CounterName = IEventCounterShortcutsConstants.CpuUsage,
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

                // We have to open EventPipeTrigger from private to internal (and _pipeline as well) to get this to work -> is that acceptable?
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
