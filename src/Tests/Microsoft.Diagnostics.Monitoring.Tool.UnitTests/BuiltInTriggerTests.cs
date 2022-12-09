// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class BuiltInTriggerTests
    {
        private readonly TimeSpan CustomSlidingWindowDuration = TimeSpan.Parse("00:00:03");
        private const double CustomLessThan = 20;

        private ITestOutputHelper _outputHelper;

        public BuiltInTriggerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void GCHeapSizeTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.GCHeapSize,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            GCHeapSizeOptions options = new()
            {
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        [Fact]
        public void ThreadpoolQueueLengthTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.ThreadpoolQueueLength,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            ThreadpoolQueueLengthOptions options = new()
            {
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        [Fact]
        public void CPUUsageTrigger_CorrectCustomOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.CPUUsage,
                GreaterThan = null,
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            CPUUsageOptions options = new()
            {
                LessThan = CustomLessThan,
                SlidingWindowDuration = CustomSlidingWindowDuration
            };

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        [Fact]
        public void GCHeapSizeTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.GCHeapSize,
                GreaterThan = GCHeapSizeOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = null // NOTE: This is populated when EventCounterOptions -> EventCounterTriggerSettings, not when GCHeapSizeOptions -> EventCounterOptions
            };

            GCHeapSizeOptions options = new();

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        [Fact]
        public void ThreadpoolQueueLengthTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.ThreadpoolQueueLength,
                GreaterThan = ThreadpoolQueueLengthOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = null // NOTE: This is populated when EventCounterOptions -> EventCounterTriggerSettings, not when ThreadpoolQueueLengthOptions -> EventCounterOptions
            };

            ThreadpoolQueueLengthOptions options = new();

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        [Fact]
        public void CPUUsageTrigger_CorrectDefaultOptions()
        {
            EventCounterOptions expectedSettings = new()
            {
                ProviderName = KnownEventCounterConstants.SystemRuntime,
                CounterName = KnownEventCounterConstants.CPUUsage,
                GreaterThan = CPUUsageOptionsDefaults.GreaterThan,
                LessThan = null,
                SlidingWindowDuration = null // NOTE: This is populated when EventCounterOptions -> EventCounterTriggerSettings, not when CPUUsageOptions -> EventCounterOptions
            };

            CPUUsageOptions options = new();

            EventCounterOptions eventCounterOptions = EventCounterTriggerFactory.ToEventCounterOptions(options);

            ValidateEventCounterOptionsTranslation(expectedSettings, eventCounterOptions);
        }

        private static void ValidateEventCounterOptionsTranslation(EventCounterOptions expected, EventCounterOptions actual)
        {
            Assert.Equal(expected.CounterName, actual.CounterName);
            Assert.Equal(expected.ProviderName, actual.ProviderName);
            Assert.Equal(expected.GreaterThan, actual.GreaterThan);
            Assert.Equal(expected.LessThan, actual.LessThan);
            Assert.Equal(expected.SlidingWindowDuration, actual.SlidingWindowDuration);
        }
    }
}
