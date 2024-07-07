// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class CollectionRuleOptionsTests
    {
        private const string DefaultRuleName = "TestRule";
        private const string UnknownEgressName = "UnknownEgress";
        private const string ExpectedMeterName = "Meter";
        private const string ExpectedInstrumentName = "Instrument";

        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleOptionsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public Task CollectionRuleOptions_MinimumOptions()
        {
            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger();
                },
                ruleOptions =>
                {
                    Assert.Empty(ruleOptions.Filters);

                    ruleOptions.VerifyStartupTrigger();

                    Assert.Empty(ruleOptions.Actions);

                    Assert.Null(ruleOptions.Limits);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_NoTrigger()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .AddExecuteAction("cmd.exe");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(CollectionRuleOptions.Trigger));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_UnknownTrigger()
        {
            const string ExpectedTriggerType = "UnknownTrigger";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetTrigger(ExpectedTriggerType);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyUnknownTriggerTypeMessage(failures, 0, ExpectedTriggerType);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_UnknownAction()
        {
            const string ExpectedActionType = "UnknownAction";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddAction(ExpectedActionType);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyUnknownActionTypeMessage(failures, 0, ExpectedActionType);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventCounterTrigger_MinimumOptions()
        {
            const string ExpectedProviderName = "System.Runtime";
            const string ExpectedCounterName = "cpu-usage";
            const double ExpectedGreaterThan = 0.5;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            options.ProviderName = ExpectedProviderName;
                            options.CounterName = ExpectedCounterName;
                            options.GreaterThan = ExpectedGreaterThan;
                        });
                },
                ruleOptions =>
                {
                    EventCounterOptions eventCounterOptions = ruleOptions.VerifyEventCounterTrigger();
                    Assert.Equal(ExpectedProviderName, eventCounterOptions.ProviderName);
                    Assert.Equal(ExpectedCounterName, eventCounterOptions.CounterName);
                    Assert.Equal(ExpectedGreaterThan, eventCounterOptions.GreaterThan);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventCounterTrigger_RoundTrip()
        {
            const string ExpectedProviderName = "System.Runtime";
            const string ExpectedCounterName = "cpu-usage";
            const double ExpectedGreaterThan = 0.5;
            const double ExpectedLessThan = 0.75;
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(30);

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            options.ProviderName = ExpectedProviderName;
                            options.CounterName = ExpectedCounterName;
                            options.GreaterThan = ExpectedGreaterThan;
                            options.LessThan = ExpectedLessThan;
                            options.SlidingWindowDuration = ExpectedDuration;
                        });
                },
                ruleOptions =>
                {
                    EventCounterOptions eventCounterOptions = ruleOptions.VerifyEventCounterTrigger();
                    Assert.Equal(ExpectedProviderName, eventCounterOptions.ProviderName);
                    Assert.Equal(ExpectedCounterName, eventCounterOptions.CounterName);
                    Assert.Equal(ExpectedGreaterThan, eventCounterOptions.GreaterThan);
                    Assert.Equal(ExpectedLessThan, eventCounterOptions.LessThan);
                    Assert.Equal(ExpectedDuration, eventCounterOptions.SlidingWindowDuration);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventCounterTrigger_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    // Property validation failures will short-circuit the remainder of the validation
                    // rules, thus only observe 3 errors when one might expect 4 (the fourth being that
                    // either GreaterThan or LessThan should be specified).
                    Assert.Equal(3, failures.Length);
                    VerifyRequiredMessage(failures, 0, nameof(EventCounterOptions.ProviderName));
                    VerifyRequiredMessage(failures, 1, nameof(EventCounterOptions.CounterName));
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(EventCounterOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventCounterTrigger_NoGreaterThanOrLessThan()
        {
            const string ExpectedProviderName = "System.Runtime";
            const string ExpectedCounterName = "cpu-usage";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            options.ProviderName = ExpectedProviderName;
                            options.CounterName = ExpectedCounterName;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEitherRequiredMessage(failures, 0,
                        nameof(EventCounterOptions.GreaterThan), nameof(EventCounterOptions.LessThan));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventCounterTrigger_GreaterThanLargerThanLessThan()
        {
            const string ExpectedProviderName = "System.Runtime";
            const string ExpectedCounterName = "cpu-usage";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            options.ProviderName = ExpectedProviderName;
                            options.CounterName = ExpectedCounterName;
                            options.GreaterThan = 0.75;
                            options.LessThan = 0.5;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFieldLessThanOtherFieldMessage(failures, 0, nameof(EventCounterOptions.GreaterThan), nameof(EventCounterOptions.LessThan));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_MinimumOptions()
        {
            const double ExpectedGreaterThan = 0.5;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                            options.GreaterThan = ExpectedGreaterThan;
                        });
                },
                ruleOptions =>
                {
                    EventMeterOptions eventMeterOptions = ruleOptions.VerifyEventMeterTrigger();
                    Assert.Equal(ExpectedMeterName, eventMeterOptions.MeterName);
                    Assert.Equal(ExpectedInstrumentName, eventMeterOptions.InstrumentName);
                    Assert.Equal(ExpectedGreaterThan, eventMeterOptions.GreaterThan);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_Default_RoundTrip()
        {
            const double ExpectedGreaterThan = 0.5;
            const double ExpectedLessThan = 0.75;
            const int ExpectedHistogramPercentile = 95;
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(30);

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                            options.GreaterThan = ExpectedGreaterThan;
                            options.LessThan = ExpectedLessThan;
                            options.SlidingWindowDuration = ExpectedDuration;
                            options.HistogramPercentile = ExpectedHistogramPercentile;
                        });
                },
                ruleOptions =>
                {
                    EventMeterOptions eventMeterOptions = ruleOptions.VerifyEventMeterTrigger();
                    Assert.Equal(ExpectedMeterName, eventMeterOptions.MeterName);
                    Assert.Equal(ExpectedInstrumentName, eventMeterOptions.InstrumentName);
                    Assert.Equal(ExpectedGreaterThan, eventMeterOptions.GreaterThan);
                    Assert.Equal(ExpectedLessThan, eventMeterOptions.LessThan);
                    Assert.Equal(ExpectedDuration, eventMeterOptions.SlidingWindowDuration);
                    Assert.Equal(ExpectedHistogramPercentile, eventMeterOptions.HistogramPercentile);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_Histogram_RoundTrip()
        {
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(30);
            int ExpectedHistogramPercentile = 50;
            int ExpectedGreaterThan = 1;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                            options.HistogramPercentile = ExpectedHistogramPercentile;
                            options.SlidingWindowDuration = ExpectedDuration;
                            options.GreaterThan = ExpectedGreaterThan;
                        });
                },
                ruleOptions =>
                {
                    EventMeterOptions eventMeterOptions = ruleOptions.VerifyEventMeterTrigger();
                    Assert.Equal(ExpectedMeterName, eventMeterOptions.MeterName);
                    Assert.Equal(ExpectedInstrumentName, eventMeterOptions.InstrumentName);
                    Assert.Equal(ExpectedHistogramPercentile, eventMeterOptions.HistogramPercentile);
                    Assert.Equal(ExpectedDuration, eventMeterOptions.SlidingWindowDuration);
                    Assert.Equal(ExpectedGreaterThan, eventMeterOptions.GreaterThan);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    // Property validation failures will short-circuit the remainder of the validation
                    // rules, thus only observe 3 errors when one might expect 4 (GreaterThan or LessThan should be specified).
                    Assert.Equal(3, failures.Length);
                    VerifyRequiredMessage(failures, 0, nameof(EventMeterOptions.MeterName));
                    VerifyRequiredMessage(failures, 1, nameof(EventMeterOptions.InstrumentName));
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(EventMeterOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_NoGreaterThanOrLessThan()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEitherRequiredMessage(failures, 0,
                        nameof(EventMeterOptions.GreaterThan), nameof(EventMeterOptions.LessThan));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_NoInstrumentOrMeterName()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.GreaterThan = 0.5;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Equal(2, failures.Length);
                    VerifyRequiredMessage(failures, 0, nameof(EventMeterOptions.MeterName));
                    VerifyRequiredMessage(failures, 1, nameof(EventMeterOptions.InstrumentName));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_EventMeterTrigger_GreaterThanLargerThanLessThan()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                            options.GreaterThan = 0.75;
                            options.LessThan = 0.5;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFieldLessThanOtherFieldMessage(failures, 0, nameof(EventMeterOptions.GreaterThan), nameof(EventMeterOptions.LessThan));
                });
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public Task CollectionRuleOptions_EventMeterTrigger_InvalidHistogramPercentile(int expectedHistogramPercentile)
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            options.MeterName = ExpectedMeterName;
                            options.InstrumentName = ExpectedInstrumentName;
                            options.GreaterThan = 0.5;
                            options.HistogramPercentile = expectedHistogramPercentile;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);

                    VerifyRangeMessage<int>(failures, 0, nameof(EventMeterOptions.HistogramPercentile),
                        TriggerOptionsConstants.Percentage_MinValue.ToString(), TriggerOptionsConstants.Percentage_MaxValue.ToString());
                });
        }

        [Theory]
        [MemberData(nameof(GetIEventCounterShortcutsAndNames))]
        public Task CollectionRuleOptions_IEventCounterTrigger_MinimumOptions(Type triggerType, string triggerName)
        {
            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetIEventCounterTrigger(triggerType, triggerName);
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyIEventCounterTrigger(triggerType, triggerName);
                });
        }

        [Theory]
        [MemberData(nameof(GetIEventCounterShortcutsAndNames))]
        public Task CollectionRuleOptions_IEventCounterTrigger_RoundTrip(Type triggerType, string triggerName)
        {
            const double ExpectedGreaterThan = 0.5;
            const double ExpectedLessThan = 0.75;
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(30);

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetIEventCounterTrigger(triggerType, triggerName, options =>
                        {
                            options.GreaterThan = ExpectedGreaterThan;
                            options.LessThan = ExpectedLessThan;
                            options.SlidingWindowDuration = ExpectedDuration;
                        });
                },
                ruleOptions =>
                {
                    IEventCounterShortcuts options = ruleOptions.VerifyIEventCounterTrigger(triggerType, triggerName);
                    Assert.Equal(ExpectedGreaterThan, options.GreaterThan);
                    Assert.Equal(ExpectedLessThan, options.LessThan);
                    Assert.Equal(ExpectedDuration, options.SlidingWindowDuration);
                });
        }

        [Theory]
        [MemberData(nameof(GetIEventCounterShortcutsAndNames))]
        public Task CollectionRuleOptions_IEventCounterTrigger_GreaterThanLargerThanLessThan(Type triggerType, string triggerName)
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetIEventCounterTrigger(triggerType, triggerName, options =>
                        {
                            options.GreaterThan = 0.75;
                            options.LessThan = 0.5;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFieldLessThanOtherFieldMessage(failures, 0, nameof(IEventCounterShortcuts.GreaterThan), nameof(IEventCounterShortcuts.LessThan));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CPUUsageTrigger_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetCPUUsageTrigger(options =>
                        {
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                            options.GreaterThan = -1;
                            options.LessThan = 101;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(3, failures.Length);
                    VerifyRangeMessage<double>(failures, 0, nameof(CPUUsageOptions.GreaterThan),
                        TriggerOptionsConstants.Percentage_MinValue.ToString(), TriggerOptionsConstants.Percentage_MaxValue.ToString());
                    VerifyRangeMessage<double>(failures, 1, nameof(CPUUsageOptions.LessThan),
                        TriggerOptionsConstants.Percentage_MinValue.ToString(), TriggerOptionsConstants.Percentage_MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(CPUUsageOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_GCHeapSizeTrigger_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetGCHeapSizeTrigger(options =>
                        {
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                            options.GreaterThan = -1;
                            options.LessThan = -1;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(3, failures.Length);
                    VerifyRangeMessage<double>(failures, 0, nameof(GCHeapSizeOptions.GreaterThan),
                        "0", double.MaxValue.ToString());
                    VerifyRangeMessage<double>(failures, 1, nameof(GCHeapSizeOptions.LessThan),
                        "0", double.MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(GCHeapSizeOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_ThreadpoolQueueLengthTrigger_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetThreadpoolQueueLengthTrigger(options =>
                        {
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                            options.GreaterThan = -1;
                            options.LessThan = -1;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(3, failures.Length);
                    VerifyRangeMessage<double>(failures, 0, nameof(ThreadpoolQueueLengthOptions.GreaterThan),
                        "0", double.MaxValue.ToString());
                    VerifyRangeMessage<double>(failures, 1, nameof(ThreadpoolQueueLengthOptions.LessThan),
                        "0", double.MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(ThreadpoolQueueLengthOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Theory]
        [MemberData(nameof(GetIEventCounterShortcutsAndNames))]
        public Task CollectionRuleOptions_IEventCounterTrigger_LessThanAssignedGreaterThanUnassigned(Type triggerType, string triggerName)
        {
            const double ExpectedLessThan = 20;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetIEventCounterTrigger(triggerType, triggerName, options =>
                        {
                            options.LessThan = ExpectedLessThan;
                        });
                },
                ruleOptions =>
                {
                    IEventCounterShortcuts options = ruleOptions.VerifyIEventCounterTrigger(triggerType, triggerName);
                    Assert.Null(options.GreaterThan);
                    Assert.Equal(ExpectedLessThan, options.LessThan);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestCountTrigger_MinimumOptions()
        {
            const int ExpectedRequestCount = 10;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestCountTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                        });
                },
                ruleOptions =>
                {
                    AspNetRequestCountOptions requestCountOptions = ruleOptions.VerifyAspNetRequestCountTrigger();
                    Assert.Equal(ExpectedRequestCount, requestCountOptions.RequestCount);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestCountTrigger_RoundTrip()
        {
            const int ExpectedRequestCount = 10;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.FromSeconds(45);
            string[] ExpectedIncludePaths = { "IncludePath1", "IncludePath2" };
            string[] ExpectedExcludePaths = { "ExcludePath1", "ExcludePath2" };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestCountTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                            options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
                            options.IncludePaths = ExpectedIncludePaths;
                            options.ExcludePaths = ExpectedExcludePaths;
                        });
                },
                ruleOptions =>
                {
                    AspNetRequestCountOptions requestCountOptions = ruleOptions.VerifyAspNetRequestCountTrigger();
                    Assert.Equal(ExpectedRequestCount, requestCountOptions.RequestCount);
                    Assert.Equal(ExpectedSlidingWindowDuration, requestCountOptions.SlidingWindowDuration);
                    Assert.Equal(ExpectedIncludePaths, requestCountOptions.IncludePaths);
                    Assert.Equal(ExpectedExcludePaths, requestCountOptions.ExcludePaths);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestCountTrigger_RequiredPropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestCountTrigger();
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Single(failures);
                    VerifyRangeMessage<int>(failures, 0, nameof(AspNetRequestCountOptions.RequestCount), "1", int.MaxValue.ToString()); // Since non-nullable, defaults to 0
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestCountTrigger_RangePropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestCountTrigger(options =>
                        {
                            options.RequestCount = -1;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(2, failures.Length);
                    VerifyRangeMessage<int>(failures, 0, nameof(AspNetRequestCountOptions.RequestCount), "1", int.MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 1, nameof(AspNetRequestCountOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestDurationTrigger_MinimumOptions()
        {
            const int ExpectedRequestCount = 10;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                        });
                },
                ruleOptions =>
                {
                    AspNetRequestDurationOptions requestDurationOptions = ruleOptions.VerifyAspNetRequestDurationTrigger();
                    Assert.Equal(ExpectedRequestCount, requestDurationOptions.RequestCount);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestDurationTrigger_RoundTrip()
        {
            const int ExpectedRequestCount = 10;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.FromSeconds(45);
            TimeSpan ExpectedRequestDuration = TimeSpan.FromSeconds(60);
            string[] ExpectedIncludePaths = { "IncludePath1", "IncludePath2" };
            string[] ExpectedExcludePaths = { "ExcludePath1", "ExcludePath2" };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                            options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
                            options.IncludePaths = ExpectedIncludePaths;
                            options.ExcludePaths = ExpectedExcludePaths;
                            options.RequestDuration = ExpectedRequestDuration;
                        });
                },
                ruleOptions =>
                {
                    AspNetRequestDurationOptions requestDurationOptions = ruleOptions.VerifyAspNetRequestDurationTrigger();
                    Assert.Equal(ExpectedRequestCount, requestDurationOptions.RequestCount);
                    Assert.Equal(ExpectedSlidingWindowDuration, requestDurationOptions.SlidingWindowDuration);
                    Assert.Equal(ExpectedIncludePaths, requestDurationOptions.IncludePaths);
                    Assert.Equal(ExpectedExcludePaths, requestDurationOptions.ExcludePaths);
                    Assert.Equal(ExpectedRequestDuration, requestDurationOptions.RequestDuration);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestDurationTrigger_RequiredPropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger();
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Single(failures);
                    VerifyRangeMessage<int>(failures, 0, nameof(AspNetRequestDurationOptions.RequestCount), "1", int.MaxValue.ToString()); // Since non-nullable, defaults to 0
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetRequestDurationTrigger_RangePropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = -1;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                            options.RequestDuration = TimeSpan.FromSeconds(-1);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(3, failures.Length);
                    VerifyRangeMessage<int>(failures, 0, nameof(AspNetRequestDurationOptions.RequestCount), "1", int.MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 1, nameof(AspNetRequestDurationOptions.RequestDuration),
                        AspNetRequestDurationOptions.RequestDuration_MinValue, AspNetRequestDurationOptions.RequestDuration_MaxValue);
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(AspNetRequestDurationOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetResponseStatusTrigger_MinimumOptions()
        {
            const int ExpectedResponseCount = 10;
            string[] ExpectedStatusCodes = { "400", "500" };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = ExpectedResponseCount;
                            options.StatusCodes = ExpectedStatusCodes;
                        });
                },
                ruleOptions =>
                {
                    AspNetResponseStatusOptions responseStatusOptions = ruleOptions.VerifyAspNetResponseStatusTrigger();
                    Assert.Equal(ExpectedResponseCount, responseStatusOptions.ResponseCount);
                    Assert.Equal(ExpectedStatusCodes, responseStatusOptions.StatusCodes);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetResponseStatusTrigger_RoundTrip()
        {
            const int ExpectedResponseCount = 10;
            TimeSpan ExpectedSlidingWindowDuration = TimeSpan.FromSeconds(45);
            string[] ExpectedIncludePaths = { "IncludePath1", "IncludePath2" };
            string[] ExpectedExcludePaths = { "ExcludePath1", "ExcludePath2" };
            string[] ExpectedStatusCodes = { "400", "500" };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = ExpectedResponseCount;
                            options.SlidingWindowDuration = ExpectedSlidingWindowDuration;
                            options.IncludePaths = ExpectedIncludePaths;
                            options.ExcludePaths = ExpectedExcludePaths;
                            options.StatusCodes = ExpectedStatusCodes;
                        });
                },
                ruleOptions =>
                {
                    AspNetResponseStatusOptions responseStatusOptions = ruleOptions.VerifyAspNetResponseStatusTrigger();
                    Assert.Equal(ExpectedResponseCount, responseStatusOptions.ResponseCount);
                    Assert.Equal(ExpectedSlidingWindowDuration, responseStatusOptions.SlidingWindowDuration);
                    Assert.Equal(ExpectedIncludePaths, responseStatusOptions.IncludePaths);
                    Assert.Equal(ExpectedExcludePaths, responseStatusOptions.ExcludePaths);
                    Assert.Equal(ExpectedStatusCodes, responseStatusOptions.StatusCodes);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetResponseStatusTrigger_RequiredPropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger();
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(2, failures.Length);
                    VerifyRequiredMessage(failures, 0, nameof(AspNetResponseStatusOptions.StatusCodes));
                    VerifyRangeMessage<int>(failures, 1, nameof(AspNetResponseStatusOptions.ResponseCount), "1", int.MaxValue.ToString()); // Since non-nullable, defaults to 0
                });
        }

        [Fact]
        public Task CollectionRuleOptions_AspNetResponseStatusTrigger_RangePropertyValidation()
        {
            string[] ExpectedStatusCodes = { "600" };

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = -1;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(-1);
                            options.StatusCodes = ExpectedStatusCodes;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();

                    Assert.Equal(3, failures.Length);
                    VerifyStatusCodesRegexMessage(failures, 0, nameof(AspNetResponseStatusOptions.StatusCodes));
                    VerifyRangeMessage<int>(failures, 1, nameof(AspNetResponseStatusOptions.ResponseCount), "1", int.MaxValue.ToString());
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(AspNetResponseStatusOptions.SlidingWindowDuration),
                        TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectDumpAction_RoundTrip()
        {
            const DumpType ExpectedDumpType = DumpType.Mini;
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectDumpAction(ExpectedEgressProvider, o => o.Type = ExpectedDumpType);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectDumpAction(0, ExpectedDumpType, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectDumpAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectDumpAction(UnknownEgressName, o => o.Type = (DumpType)20);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Equal(2, failures.Length);
                    VerifyEnumDataTypeMessage<DumpType>(failures, 0, nameof(CollectDumpOptions.Type));
                    VerifyEgressNotExistMessage(failures, 1, UnknownEgressName);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectGCDumpAction_RoundTrip()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectGCDumpAction(ExpectedEgressProvider);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectGCDumpAction(0, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectGCDumpAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectGCDumpAction(UnknownEgressName);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEgressNotExistMessage(failures, 0, UnknownEgressName);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLogsAction_MinimumOptions()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLogsAction(ExpectedEgressProvider);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectLogsAction(0, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLogsAction_RoundTrip()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const bool ExpectedUseAppFilters = true;
            const LogLevel ExpectedLogLevel = LogLevel.Debug;
            Dictionary<string, LogLevel?> ExpectedFilterSpecs = new()
            {
                { "CategoryA", LogLevel.Information },
                { "CategoryA.SubCategoryA", LogLevel.Warning }
            };
            TimeSpan ExpectedDuration = TimeSpan.FromMinutes(2);

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLogsAction(ExpectedEgressProvider, options =>
                        {
                            options.UseAppFilters = ExpectedUseAppFilters;
                            options.DefaultLevel = ExpectedLogLevel;
                            options.FilterSpecs = ExpectedFilterSpecs;
                            options.Duration = ExpectedDuration;
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    CollectLogsOptions collectLogsOptions = ruleOptions.VerifyCollectLogsAction(0, ExpectedEgressProvider);
                    Assert.Equal(ExpectedUseAppFilters, collectLogsOptions.UseAppFilters);
                    Assert.Equal(ExpectedLogLevel, collectLogsOptions.DefaultLevel);
                    Assert.NotNull(collectLogsOptions.FilterSpecs);
                    Assert.Equal(ExpectedFilterSpecs.Count, collectLogsOptions.FilterSpecs.Count);
                    foreach ((string expectedCategory, LogLevel? expectedLogLevel) in ExpectedFilterSpecs)
                    {
                        Assert.True(collectLogsOptions.FilterSpecs.TryGetValue(expectedCategory, out LogLevel? actualLogLevel));
                        Assert.Equal(expectedLogLevel, actualLogLevel);
                    }
                    Assert.Equal(ExpectedDuration, collectLogsOptions.Duration);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLogsAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLogsAction(egress: null, options =>
                        {
                            options.DefaultLevel = (LogLevel)100;
                            options.Duration = TimeSpan.FromDays(3);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Equal(3, failures.Length);
                    VerifyEnumDataTypeMessage<LogLevel>(failures, 0, nameof(CollectLogsOptions.DefaultLevel));
                    VerifyRangeMessage<TimeSpan>(failures, 1, nameof(CollectLogsOptions.Duration),
                        ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue);
                    VerifyRequiredOrDefaultEgressProvider(failures, 2);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLogsAction_FilterSpecValidation()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLogsAction(ExpectedEgressProvider, options =>
                        {
                            options.FilterSpecs = new Dictionary<string, LogLevel?>()
                            {
                                { "CategoryA", (LogLevel)50 }
                            };
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEnumDataTypeMessage<LogLevel>(failures, 0, nameof(CollectLogsOptions.FilterSpecs));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_MinimumOptions_Profile()
        {
            const TraceProfile ExpectedProfile = TraceProfile.Http;
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProfile, ExpectedEgressProvider);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectTraceAction(0, ExpectedProfile, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_MinimumOptions_Providers()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Name = "Microsoft-Extensions-Logging" }
            };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectTraceAction(0, ExpectedProviders, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_RoundTrip_Profile()
        {
            const TraceProfile ExpectedProfile = TraceProfile.Logs;
            const string ExpectedEgressProvider = "TmpEgressProvider";
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(45);

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProfile, ExpectedEgressProvider, options =>
                        {
                            options.Duration = ExpectedDuration;
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    CollectTraceOptions collectTraceOptions = ruleOptions.VerifyCollectTraceAction(0, ExpectedProfile, ExpectedEgressProvider);

                    Assert.Equal(ExpectedDuration, collectTraceOptions.Duration);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_RoundTrip_Providers()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new()
                {
                    Name = "Microsoft-Extensions-Logging",
                    EventLevel = EventLevel.Warning,
                    Keywords = "0xC",
                    Arguments = new Dictionary<string, string>()
                    {
                        { "FilterSpecs", "UseAppFilters" }
                    }
                }
            };
            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(20);
            const int ExpectedBufferSizeMegabytes = 128;
            const bool ExpectedRequestRundown = false;

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider, options =>
                        {
                            options.BufferSizeMegabytes = ExpectedBufferSizeMegabytes;
                            options.Duration = ExpectedDuration;
                            options.RequestRundown = ExpectedRequestRundown;
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    CollectTraceOptions collectTraceOptions = ruleOptions.VerifyCollectTraceAction(0, ExpectedProviders, ExpectedEgressProvider);

                    Assert.Equal(ExpectedBufferSizeMegabytes, collectTraceOptions.BufferSizeMegabytes);
                    Assert.Equal(ExpectedDuration, collectTraceOptions.Duration);
                    Assert.Equal(ExpectedRequestRundown, collectTraceOptions.RequestRundown);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction((TraceProfile)75, egress: null, options =>
                        {
                            options.BufferSizeMegabytes = 2048;
                            options.Duration = TimeSpan.FromDays(7);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Equal(4, failures.Length);
                    VerifyEnumDataTypeMessage<TraceProfile>(failures, 0, nameof(CollectTraceOptions.Profile));
                    VerifyRangeMessage<int>(failures, 1, nameof(CollectTraceOptions.BufferSizeMegabytes),
                        ActionOptionsConstants.BufferSizeMegabytes_MinValue_String, ActionOptionsConstants.BufferSizeMegabytes_MaxValue_String);
                    VerifyRangeMessage<TimeSpan>(failures, 2, nameof(CollectTraceOptions.Duration),
                        ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue);
                    VerifyRequiredOrDefaultEgressProvider(failures, 3);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_ValidateProviderIntervals()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const int ExpectedInterval = 7;

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.AddGlobalCounter(5);
                    rootOptions.AddProviderInterval(MonitoringSourceConfiguration.SystemRuntimeEventSourceName, ExpectedInterval);

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");

                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(new EventPipeProvider[] { new EventPipeProvider
                        {
                            Name = MonitoringSourceConfiguration.SystemRuntimeEventSourceName,
                            Arguments = new Dictionary<string, string>{ { "EventCounterIntervalSec", "5" } },
                        }},
                        ExpectedEgressProvider, null);
                },
                ex =>
                {
                    string failure = Assert.Single(ex.Failures);
                    VerifyProviderIntervalMessage(failure, MonitoringSourceConfiguration.SystemRuntimeEventSourceName, ExpectedInterval);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_InvalidProviderInterval()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.AddGlobalCounter(5);
                    rootOptions.AddProviderInterval(MonitoringSourceConfiguration.SystemRuntimeEventSourceName, -2);

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");

                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(new EventPipeProvider[] { new EventPipeProvider
                        {
                            Name = MonitoringSourceConfiguration.SystemRuntimeEventSourceName,
                            Arguments = new Dictionary<string, string>{ { "EventCounterIntervalSec", "5" } },
                        }},
                        ExpectedEgressProvider, null);
                },
                ex =>
                {
                    string failure = Assert.Single(ex.Failures);
                    VerifyNestedGlobalInterval(failure, MonitoringSourceConfiguration.SystemRuntimeEventSourceName);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_NoProfileOrProviders()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(Enumerable.Empty<EventPipeProvider>(), ExpectedEgressProvider);

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEitherRequiredMessage(failures, 0,
                        nameof(CollectTraceOptions.Profile), nameof(CollectTraceOptions.Providers));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_BothProfileAndProviders()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(TraceProfile.Metrics, ExpectedEgressProvider, options =>
                        {
                            options.Providers = new List<EventPipeProvider>()
                            {
                                new() { Name = "Microsoft-Extensions-Logging" }
                            };
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyBothCannotBeSpecifiedMessage(failures, 0,
                        nameof(CollectTraceOptions.Profile), nameof(CollectTraceOptions.Providers));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_ProviderPropertyValidation()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Keywords = null }
            };

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider);

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(EventPipeProvider.Name));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_StopOnEvent()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const string ExpectedEventProviderName = "Microsoft-Extensions-Logging";
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Name = ExpectedEventProviderName }
            };

            TraceEventFilter expectedStoppingEvent = new()
            {
                EventName = "CustomEvent",
                ProviderName = ExpectedEventProviderName
            };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider, (options) =>
                        {
                            options.StoppingEvent = expectedStoppingEvent;
                        });
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectTraceAction(0, ExpectedProviders, ExpectedEgressProvider, expectedStoppingEvent);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_StopOnEvent_MissingProviderConfig()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const string ExpectedMissingEventProviderName = "Non-Existent-Provider";

            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Name = "Microsoft-Extensions-Logging" }
            };

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider, (options) =>
                        {
                            options.StoppingEvent = new TraceEventFilter()
                            {
                                EventName = "CustomEvent",
                                ProviderName = ExpectedMissingEventProviderName
                            };
                        });
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyMissingStoppingEventProviderMessage(
                        failures,
                        0,
                        nameof(CollectTraceOptions.StoppingEvent),
                        ExpectedMissingEventProviderName,
                        nameof(CollectTraceOptions.Providers));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectTraceAction_BothProfileAndStoppingEvent()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(TraceProfile.Metrics, ExpectedEgressProvider, options =>
                        {
                            options.StoppingEvent = new TraceEventFilter()
                            {
                                EventName = "CustomEvent",
                                ProviderName = "CustomProvider"
                            };
                        });

                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.NotEmpty(failures);
                    VerifyBothCannotBeSpecifiedMessage(
                        failures,
                        0,
                        nameof(CollectTraceOptions.Profile),
                        nameof(CollectTraceOptions.StoppingEvent));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLiveMetricsAction_RoundTrip()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const bool ExpectedIncludeDefaultProviders = false;

            TimeSpan ExpectedDuration = TimeSpan.FromSeconds(45);

            const string providerName = EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName;
            var counterNames = new[] { "cpu-usage", "working-set" };

            const string ExpectedMeterName = "myMeter";
            var ExpectedInstrumentNames = new[] { "thisGauge", "thatHistogram" };

            EventMetricsProvider[] ExpectedProviders = new[]
            {
                new EventMetricsProvider
                {
                    ProviderName = providerName,
                    CounterNames = counterNames,
                }
            };

            EventMetricsMeter[] ExpectedMeters = new[]
            {
                new EventMetricsMeter
                {
                    MeterName = ExpectedMeterName,
                    InstrumentNames = ExpectedInstrumentNames
                }
            };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLiveMetricsAction(ExpectedEgressProvider, options =>
                        {
                            options.Duration = ExpectedDuration;
                            options.IncludeDefaultProviders = ExpectedIncludeDefaultProviders;
                            options.Providers = ExpectedProviders;
                            options.Meters = ExpectedMeters;
                        });
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp");
                },
                ruleOptions =>
                {
                    CollectLiveMetricsOptions collectLiveMetricsOptions = ruleOptions.VerifyCollectLiveMetricsAction(0, ExpectedEgressProvider);

                    Assert.Equal(ExpectedDuration, collectLiveMetricsOptions.Duration);
                    Assert.Equal(ExpectedIncludeDefaultProviders, collectLiveMetricsOptions.IncludeDefaultProviders);
                    Assert.Equal(ExpectedProviders.Select(x => x.CounterNames.ToHashSet()), collectLiveMetricsOptions.Providers.Select(x => x.CounterNames.ToHashSet()));
                    Assert.Equal(ExpectedProviders.Select(x => x.ProviderName), collectLiveMetricsOptions.Providers.Select(x => x.ProviderName));
                    Assert.Equal(ExpectedMeters.Select(x => x.InstrumentNames.ToHashSet()), collectLiveMetricsOptions.Meters.Select(x => x.InstrumentNames.ToHashSet()));
                    Assert.Equal(ExpectedMeters.Select(x => x.MeterName), collectLiveMetricsOptions.Meters.Select(x => x.MeterName));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectLiveMetricsAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectLiveMetricsAction(UnknownEgressName, options =>
                        {
                            options.Duration = TimeSpan.FromDays(3);
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Equal(2, failures.Length);
                    VerifyRangeMessage<TimeSpan>(failures, 0, nameof(CollectTraceOptions.Duration),
                        ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue);
                    VerifyEgressNotExistMessage(failures, 1, UnknownEgressName);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_ExecuteAction_MinimumOptions()
        {
            const string ExpectedExePath = "cmd.exe";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddExecuteAction(ExpectedExePath);
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifyExecuteAction(0, ExpectedExePath);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_ExecuteAction_RoundTrip()
        {
            const string ExpectedExePath = "cmd.exe";
            const string ExpectedArguments = "/c \"echo Hello\"";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddExecuteAction(ExpectedExePath, ExpectedArguments);
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifyExecuteAction(0, ExpectedExePath, ExpectedArguments);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_ExecuteAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddExecuteAction(path: null, "arg1 arg2");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(ExecuteOptions.Path));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_LoadProfilerAction_RoundTrip()
        {
            const string ExpectedTargetPath = @"C:\My\Path\To\CorProfiler.dll";
            Guid ExpectedClsid = Guid.NewGuid();

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddLoadProfilerAction(
                        callback: opts =>
                        {
                            opts.Path = ExpectedTargetPath;
                            opts.Clsid = ExpectedClsid;
                        });
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifyLoadProfilerAction(0, ExpectedTargetPath, ExpectedClsid);
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_LoadProfilerAction_PathPropertyValidation()
        {
            Guid ExpectedClsid = Guid.NewGuid();
            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddLoadProfilerAction(
                        callback: opts =>
                        {
                            opts.Path = null;
                            opts.Clsid = ExpectedClsid;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(LoadProfilerOptions.Path));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddLoadProfilerAction(
                        callback: opts =>
                        {
                            opts.Path = string.Empty;
                            opts.Clsid = ExpectedClsid;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(LoadProfilerOptions.Path));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddLoadProfilerAction(
                        callback: opts =>
                        {
                            opts.Path = "   "; // White space is not allowed by the [Required] Attribute
                            opts.Clsid = ExpectedClsid;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(LoadProfilerOptions.Path));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_LoadProfilerAction_ClsidPropertyValidation()
        {
            const string ExpectedTargetPath = @"C:\My\Path\To\CorProfiler.dll";
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddLoadProfilerAction(
                        callback: opts =>
                        {
                            opts.Path = ExpectedTargetPath;
                            opts.Clsid = Guid.Empty;
                        });
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredGuidMessage(failures, 0, nameof(LoadProfilerOptions.Clsid));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_SetEnvironmentVariable_MinimumOptions()
        {
            const string VariableName = "MyProfiler_OptionA";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(VariableName);
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifySetEnvironmentVariableAction(0, VariableName, null);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_SetEnvironmentVariable_RoundTrip()
        {
            const string VariableName = "MyProfiler_OptionA";
            const string VariableValue = "MyValue!@#$%^&*()_+-{}{]|";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(VariableName, VariableValue);
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifySetEnvironmentVariableAction(0, VariableName, VariableValue);
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_SetEnvironmentVariable_NamePropertyValidation()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(null);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(SetEnvironmentVariableOptions.Name));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(string.Empty);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(SetEnvironmentVariableOptions.Name));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction("    ");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(SetEnvironmentVariableOptions.Name));
                });
        }

        [Fact]
        public Task CollectionRuleOptions_GetEnvironmentVariable_RoundTrip()
        {
            const string VariableName = "MyGettingVar";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddGetEnvironmentVariableAction(VariableName);
                },
                ruleOptions =>
                {
                    Assert.Single(ruleOptions.Actions);
                    ruleOptions.VerifyGetEnvironmentVariableAction(0, VariableName);
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_GetEnvironmentVariable_NamePropertyValidation()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddGetEnvironmentVariableAction(null);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(GetEnvironmentVariableOptions.Name));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddGetEnvironmentVariableAction(string.Empty);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(GetEnvironmentVariableOptions.Name));
                });

            await ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddGetEnvironmentVariableAction("    ");
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyRequiredMessage(failures, 0, nameof(GetEnvironmentVariableOptions.Name));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectStacksAction_NotEnabled()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectStacksAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectStacksAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectStacksAction_DisabledViaCallStacks()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .DisableCallStacks() // Make feature unavailable
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectStacksAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectStacksAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectStacksAction_DisabledViaInProcessFeatures()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .DisableInProcessFeatures() // Make all in-process features unavailable
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectStacksAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectStacksAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectStacksAction_EnabledViaCallStacks()
        {
            await ValidateSuccess(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .EnableCallStacks()
                        .CreateCollectionRule(DefaultRuleName)
                        .SetCPUUsageTrigger(usageOptions => { usageOptions.GreaterThan = 100; })
                        .AddCollectStacksAction(fileEgress);
                },
                ruleOptions =>
                {
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectStacksAction_EnabledViaInProcessFeatures()
        {
            await ValidateSuccess(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .EnableInProcessFeatures()
                        .CreateCollectionRule(DefaultRuleName)
                        .SetCPUUsageTrigger(usageOptions => { usageOptions.GreaterThan = 100; })
                        .AddCollectStacksAction(fileEgress);
                },
                ruleOptions =>
                {
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectExceptionsAction_MinimumOptions()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(ExpectedEgressProvider);
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp")
                        .EnableExceptions()
;
                },
                ruleOptions =>
                {
                    ruleOptions.VerifyCollectExceptionsAction(0, ExpectedEgressProvider);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectExceptionsAction_RoundTrip()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const ExceptionFormat ExpectedFormat = ExceptionFormat.PlainText;
            ExceptionsConfiguration ExpectedFilters = new ExceptionsConfiguration()
            {
                Include = new() {
                    new()
                    {
                        ExceptionType = nameof(InvalidOperationException)
                    }
                },
                Exclude = new()
                {
                    new()
                    {
                        MethodName = "MyMethodName"
                    }
                }
            };

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(ExpectedEgressProvider, o =>
                        {
                            o.Filters = ExpectedFilters;
                            o.Format = ExpectedFormat;
                        });
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, "/tmp")
                        .EnableExceptions();
                },
                ruleOptions =>
                {
                    CollectExceptionsOptions collectExceptionsOptions = ruleOptions.VerifyCollectExceptionsAction(0, ExpectedEgressProvider);
                    Assert.Equal(ExpectedFormat, collectExceptionsOptions.Format);

                    ExceptionFilter ActualInclude = Assert.Single(ExpectedFilters.Include);
                    Assert.Equal(ExpectedFilters.Include.First(), ActualInclude);

                    ExceptionFilter ActualExclude = Assert.Single(ExpectedFilters.Exclude);
                    Assert.Equal(ExpectedFilters.Exclude.First(), ActualExclude);
                });
        }

        [Fact]
        public Task CollectionRuleOptions_CollectExceptionsAction_PropertyValidation()
        {
            return ValidateFailure(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(UnknownEgressName);

                    rootOptions.EnableExceptions();
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyEgressNotExistMessage(failures, 0, UnknownEgressName);
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectExceptionsAction_NotEnabled()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectExceptionsAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectExceptionsAction_DisabledViaExceptions()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .DisableExceptions() // Make feature unavailable
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectExceptionsAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectExceptionsAction_DisabledViaInProcessFeatures()
        {
            await ValidateFailure(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .DisableInProcessFeatures() // Make all in-process features unavailable
                        .CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectExceptionsAction(fileEgress);
                },
                ex =>
                {
                    string[] failures = ex.Failures.ToArray();
                    Assert.Single(failures);
                    VerifyFeatureDisabled(failures, 0, nameof(Tools.Monitor.CollectionRules.Actions.CollectExceptionsAction));
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectExceptionsAction_EnabledViaExceptions()
        {
            await ValidateSuccess(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .EnableExceptions()
                        .CreateCollectionRule(DefaultRuleName)
                        .SetCPUUsageTrigger(usageOptions => { usageOptions.GreaterThan = 100; })
                        .AddCollectExceptionsAction(fileEgress);
                },
                ruleOptions =>
                {
                });
        }

        [Fact]
        public async Task CollectionRuleOptions_CollectExceptionsAction_EnabledViaInProcessFeatures()
        {
            await ValidateSuccess(
                rootOptions =>
                {
                    const string fileEgress = nameof(fileEgress);
                    rootOptions.AddFileSystemEgress(fileEgress, "/tmp")
                        .EnableInProcessFeatures()
                        .CreateCollectionRule(DefaultRuleName)
                        .SetCPUUsageTrigger(usageOptions => { usageOptions.GreaterThan = 100; })
                        .AddCollectExceptionsAction(fileEgress);
                },
                ruleOptions =>
                {
                });
        }

        public static IEnumerable<object[]> GetIEventCounterShortcutsAndNames()
        {
            yield return new object[] { typeof(CPUUsageOptions), KnownCollectionRuleTriggers.CPUUsage };
            yield return new object[] { typeof(GCHeapSizeOptions), KnownCollectionRuleTriggers.GCHeapSize };
            yield return new object[] { typeof(ThreadpoolQueueLengthOptions), KnownCollectionRuleTriggers.ThreadpoolQueueLength };
        }

        private Task Validate(
            Action<RootOptions> setup,
            Action<IOptionsMonitor<CollectionRuleOptions>> validate,
            Action<IServiceCollection> servicesCallback = null)
        {
            return TestHostHelper.CreateCollectionRulesHost(
                _outputHelper,
                setup,
                servicesCallback: servicesCallback,
                hostCallback: host => validate(host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>()));
        }

        private Task ValidateSuccess(
            Action<RootOptions> setup,
            Action<CollectionRuleOptions> validate,
            Action<IServiceCollection> servicesCallback = null)
        {
            return Validate(
                setup,
                monitor => validate(monitor.Get(DefaultRuleName)),
                servicesCallback);
        }

        private Task ValidateFailure(
            Action<RootOptions> setup,
            Action<OptionsValidationException> validate,
            Action<IServiceCollection> servicesCallback = null)
        {
            return Validate(
                setup,
                monitor =>
                {
                    OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => monitor.Get(DefaultRuleName));
                    _outputHelper.WriteLine("Exception: {0}", ex.Message);
                    validate(ex);
                },
                servicesCallback);
        }

        private static void VerifyUnknownActionTypeMessage(string[] failures, int index, string actionType)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_UnknownActionType,
                actionType);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyUnknownTriggerTypeMessage(string[] failures, int index, string triggerType)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_UnknownTriggerType,
                triggerType);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyRequiredMessage(string[] failures, int index, string fieldName)
        {
            string message = (new RequiredAttribute()).FormatErrorMessage(fieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyRequiredOrDefaultEgressProvider(string[] failures, int index)
        {
            Assert.Equal(WebApi.OptionsDisplayStrings.ErrorMessage_NoDefaultEgressProvider, failures[index]);
        }

        private static void VerifyRequiredGuidMessage(string[] failures, int index, string fieldName)
        {
            string message = (new RequiredGuidAttribute()).FormatErrorMessage(fieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyEnumDataTypeMessage<T>(string[] failures, int index, string fieldName)
        {
            string message = (new EnumDataTypeAttribute(typeof(T))).FormatErrorMessage(fieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyRangeMessage<T>(string[] failures, int index, string fieldName, string min, string max)
        {
            string message = (new RangeAttribute(typeof(T), min, max)).FormatErrorMessage(fieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyEitherRequiredMessage(string[] failures, int index, string fieldName1, string fieldName2)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_TwoFieldsMissing,
                fieldName1,
                fieldName2);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyStatusCodesRegexMessage(string[] failures, int index, string fieldName)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                WebApi.OptionsDisplayStrings.ErrorMessage_StatusCodesRegularExpressionDoesNotMatch,
                fieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyBothCannotBeSpecifiedMessage(string[] failures, int index, string fieldName1, string fieldName2)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                fieldName1,
                fieldName2);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyFieldLessThanOtherFieldMessage(string[] failures, int index, string fieldName1, string fieldName2)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_FieldMustBeLessThanOtherField,
                fieldName1,
                fieldName2);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyEgressNotExistMessage(string[] failures, int index, string egressProvider)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_EgressProviderDoesNotExist,
                egressProvider);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyFeatureDisabled(string[] failures, int index, string featureName)
        {
            string expectedMessage = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_DisabledFeature,
                featureName);

            Assert.Equal(expectedMessage, failures[index]);
        }

        private static void VerifyMissingStoppingEventProviderMessage(string[] failures, int index, string fieldName, string providerName, string providerFieldName)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Strings.ErrorMessage_MissingStoppingEventProvider,
                fieldName,
                providerName,
                providerFieldName);

            Assert.Equal(message, failures[index]);
        }

        private static void VerifyProviderIntervalMessage(string failure, string provider, int expectedInterval)
        {
            string message = string.Format(CultureInfo.CurrentCulture, WebApi.Strings.ErrorMessage_InvalidMetricInterval, provider, expectedInterval);

            Assert.Equal(message, failure);
        }

        private static void VerifyNestedGlobalInterval(string failure, string provider)
        {
            string rangeValidationMessage = typeof(WebApi.GlobalProviderOptions)
                .GetProperty(nameof(WebApi.GlobalProviderOptions.IntervalSeconds))
                .GetCustomAttribute<RangeAttribute>()
                .FormatErrorMessage(nameof(WebApi.GlobalProviderOptions.IntervalSeconds));

            string message = string.Format(CultureInfo.CurrentCulture, WebApi.OptionsDisplayStrings.ErrorMessage_NestedProviderValidationError, provider, rangeValidationMessage);
            Assert.Equal(message, failure);
        }
    }
}
