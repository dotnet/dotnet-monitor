// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class CollectionRuleDefaultsTests
    {
        private const string DefaultRuleName = nameof(CollectionRuleDefaultsTests);

        private ITestOutputHelper _outputHelper;

        public CollectionRuleDefaultsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // QUESTION: Do we want to test this for every type of action? Technically, a regression could happen for something other
        // than CollectDump, and we wouldn't detect it with the current test.
        [Fact]
        public async Task DefaultEgress_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetActionDefaults(egress: ActionTestsConstants.ExpectedEgressProvider);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction()
                    .SetStartupTrigger();
            }, host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                Assert.Equal(ActionTestsConstants.ExpectedEgressProvider, options.Egress);
            });
        }

        [Fact]
        public async Task DefaultEgress_Failure()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction()
                    .SetStartupTrigger();
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => ActionTestsHelper.GetActionOptions<OptionsValidationException>(host, DefaultRuleName));

                Assert.Equal(string.Format(OptionsDisplayStrings.ErrorMessage_NoDefaultEgressProvider), invalidOptionsException.Message);
            });
        }

        [Fact]
        public async Task DefaultEgress_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetActionDefaults(egress: ActionTestsConstants.UnknownEgressProvider);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger();
            }, host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                Assert.Equal(ActionTestsConstants.ExpectedEgressProvider, options.Egress);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestCount_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(requestCount: TriggerTestsConstants.ExpectedRequestCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestCountTrigger();
            }, host =>
            {
                AspNetRequestCountOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedRequestCount, options.RequestCount);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestCount_Failure()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestCountTrigger();
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName));

                VerifyRangeMessage<int>(invalidOptionsException, nameof(AspNetRequestCountOptions.RequestCount), "1", int.MaxValue.ToString());
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestCount_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(requestCount: TriggerTestsConstants.UnknownRequestCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestCountTrigger(options =>
                    {
                        options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;
                    });
            }, host =>
            {
                AspNetRequestCountOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedRequestCount, options.RequestCount);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestDuration_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(requestCount: TriggerTestsConstants.ExpectedRequestCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestDurationTrigger();
            }, host =>
            {
                AspNetRequestDurationOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestDurationOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedRequestCount, options.RequestCount);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestDuration_Failure()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestDurationTrigger();
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetRequestDurationOptions>(host, DefaultRuleName));

                VerifyRangeMessage<int>(invalidOptionsException, nameof(AspNetRequestDurationOptions.RequestCount), "1", int.MaxValue.ToString());
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestDuration_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(requestCount: TriggerTestsConstants.UnknownRequestCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestDurationTrigger(options =>
                    {
                        options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;
                    });
            }, host =>
            {
                AspNetRequestDurationOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestDurationOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedRequestCount, options.RequestCount);
            });
        }

        [Fact]
        public async Task DefaultResponseCount_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(responseCount: TriggerTestsConstants.ExpectedResponseCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetResponseStatusTrigger(options =>
                    {
                        options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;
                    });
            }, host =>
            {
                AspNetResponseStatusOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetResponseStatusOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedResponseCount, options.ResponseCount);
            });
        }

        [Fact]
        public async Task DefaultResponseCount_Failure()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetResponseStatusTrigger(options =>
                    {
                        options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;
                    });
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetResponseStatusOptions>(host, DefaultRuleName));

                VerifyRangeMessage<int>(invalidOptionsException, nameof(AspNetResponseStatusOptions.ResponseCount), "1", int.MaxValue.ToString());
            });
        }

        [Fact]
        public async Task DefaultResponseCount_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(responseCount: TriggerTestsConstants.UnknownResponseCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetResponseStatusTrigger(options =>
                    {
                        options.ResponseCount = TriggerTestsConstants.ExpectedResponseCount;
                        options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;
                    });
            }, host =>
            {
                AspNetResponseStatusOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetResponseStatusOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedResponseCount, options.ResponseCount);
            });
        }

        // QUESTION: Do we want to test this for every type of trigger? Technically, a regression could happen for something other
        // than AspNetRequestCount, and we wouldn't detect it with the current test.
        [Fact]
        public async Task DefaultSlidingWindowDuration_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(slidingWindowDuration: TriggerTestsConstants.ExpectedSlidingWindowDuration);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestCountTrigger(options =>
                    {
                        options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;
                    });
            }, host =>
            {
                AspNetRequestCountOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration), options.SlidingWindowDuration);
            });
        }

        [Fact]
        public async Task DefaultSlidingWindowDuration_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetTriggerDefaults(slidingWindowDuration: TriggerTestsConstants.UnknownSlidingWindowDuration);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetAspNetRequestCountTrigger(options =>
                    {
                        options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;
                        options.SlidingWindowDuration = TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration);
                    });
            }, host =>
            {
                AspNetRequestCountOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration), options.SlidingWindowDuration);
            });
        }

        [Fact]
        public async Task DefaultActionCount_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(count: LimitsTestsConstants.ExpectedActionCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger();
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(LimitsTestsConstants.ExpectedActionCount, options.ActionCount);
            });
        }

        [Fact]
        public async Task DefaultActionCount_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(count: LimitsTestsConstants.UnknownActionCount);
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger()
                    .SetLimits(options =>
                    {
                        options.ActionCount = LimitsTestsConstants.ExpectedActionCount;
                    });
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(LimitsTestsConstants.ExpectedActionCount, options.ActionCount);
            });
        }
        [Fact]
        public async Task DefaultActionCountSlidingWindowDuration_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(slidingWindowDuration: TimeSpan.Parse(LimitsTestsConstants.ExpectedActionCountSlidingWindowDuration));
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger();
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(LimitsTestsConstants.ExpectedActionCountSlidingWindowDuration), options.ActionCountSlidingWindowDuration);
            });
        }

        [Fact]
        public async Task DefaultActionCountSlidingWindowDuration_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(slidingWindowDuration: TimeSpan.Parse(LimitsTestsConstants.UnknownActionCountSlidingWindowDuration));
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger()
                    .SetLimits(options =>
                    {
                        options.ActionCountSlidingWindowDuration = TimeSpan.Parse(LimitsTestsConstants.ExpectedActionCountSlidingWindowDuration);
                    });
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(LimitsTestsConstants.ExpectedActionCountSlidingWindowDuration), options.ActionCountSlidingWindowDuration);
            });
        }

        [Fact]
        public async Task DefaultRuleDuration_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(ruleDuration: TimeSpan.Parse(LimitsTestsConstants.ExpectedRuleDuration));
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger();
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(LimitsTestsConstants.ExpectedRuleDuration), options.RuleDuration);
            });
        }

        [Fact]
        public async Task DefaultRuleDuration_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddCollectionRuleDefaults(options =>
                {
                    options.SetLimitsDefaults(ruleDuration: TimeSpan.Parse(LimitsTestsConstants.UnknownRuleDuration));
                });

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetStartupTrigger()
                    .SetLimits(options =>
                    {
                        options.RuleDuration = TimeSpan.Parse(LimitsTestsConstants.ExpectedRuleDuration);
                    });
            }, host =>
            {
                CollectionRuleLimitsOptions options = LimitsTestsHelper.GetLimitsOptions(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(LimitsTestsConstants.ExpectedRuleDuration), options.RuleDuration);
            });
        }

        private static void VerifyRangeMessage<T>(Exception ex, string fieldName, string min, string max)
        {
            string message = (new RangeAttribute(typeof(T), min, max)).FormatErrorMessage(fieldName);

            Assert.Equal(message, ex.Message);
        }
    }
}
