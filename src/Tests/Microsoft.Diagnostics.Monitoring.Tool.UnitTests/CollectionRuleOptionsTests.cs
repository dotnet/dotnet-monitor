﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Tracing;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectionRuleOptionsTests
    {
        private const string DefaultRuleName = "TestRule";
        private const string UnknownEgressName = "UnknownEgress";

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
        public Task CollectionRuleOptions_CollectDumpAction_RoundTrip()
        {
            const DumpType ExpectedDumpType = DumpType.Mini;
            const string ExpectedEgressProvider = "TmpEgressProvider";

            return ValidateSuccess(
                rootOptions =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectDumpAction(ExpectedEgressProvider, ExpectedDumpType);
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
                        .AddCollectDumpAction(UnknownEgressName, (DumpType)20);
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
                    VerifyRequiredMessage(failures, 2, nameof(CollectLogsOptions.Egress));
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
                    VerifyRequiredMessage(failures, 3, nameof(CollectTraceOptions.Egress));
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
                        configureOptions: opts =>
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
                        configureOptions: opts =>
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
                        configureOptions: opts =>
                        {
                            opts.Path = String.Empty;
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
                        configureOptions: opts =>
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
                        configureOptions: opts =>
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

        private Task Validate(
            Action<RootOptions> setup,
            Action<IOptionsMonitor<CollectionRuleOptions>> validate)
        {
            return TestHostHelper.CreateCollectionRulesHost(
                _outputHelper,
                setup,
                host => validate(host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>()));
        }

        private Task ValidateSuccess(
            Action<RootOptions> setup,
            Action<CollectionRuleOptions> validate)
        {
            return Validate(
                setup,
                monitor => validate(monitor.Get(DefaultRuleName)));
        }

        private Task ValidateFailure(
            Action<RootOptions> setup,
            Action<OptionsValidationException> validate)
        {
            return Validate(
                setup,
                monitor =>
                {
                    OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => monitor.Get(DefaultRuleName));
                    _outputHelper.WriteLine("Exception: {0}", ex.Message);
                    validate(ex);
                });
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
    }
}
