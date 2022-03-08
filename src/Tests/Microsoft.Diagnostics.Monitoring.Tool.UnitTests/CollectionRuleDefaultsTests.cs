// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectionRuleDefaultsTests
    {
        private const string DefaultRuleName = nameof(CollectionRuleDefaultsTests);
        private const string UnknownEgressName = "UnknownEgress";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectionRuleDefaultsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Fact]
        public async Task DefaultEgress_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetEgress(ActionTestsConstants.ExpectedEgressProvider);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction() // Omit egress provider
                    .SetStartupTrigger();
            }, host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                Assert.Equal(options.Egress, ActionTestsConstants.ExpectedEgressProvider);
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
                    .AddCollectDumpAction() // Omit egress provider
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
                rootOptions.CreateCollectionRuleDefaults().SetEgress(UnknownEgressName);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider) // Override the default
                    .SetStartupTrigger();
            }, host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                Assert.Equal(options.Egress, ActionTestsConstants.ExpectedEgressProvider);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestCount_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetRequestCount(TriggerTestsConstants.ExpectedRequestCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestCount"); // Can we push this out to reading from a field?
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
                    .SetTrigger("AspNetRequestCount"); // Omit Request Count and don't set a default
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName));

                Assert.Equal(string.Format(OptionsDisplayStrings.ErrorMessage_NoDefaultRequestCount), invalidOptionsException.Message);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestCount_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetRequestCount(TriggerTestsConstants.UnknownRequestCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetRequestCountOptions options = new AspNetRequestCountOptions();
                options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestCount")
                    .Trigger.Settings = options;
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
                rootOptions.CreateCollectionRuleDefaults().SetRequestCount(TriggerTestsConstants.ExpectedRequestCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestDuration"); // Can we push this out to reading from a field?
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
                    .SetTrigger("AspNetRequestDuration"); // Omit Request Count and don't set a default
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetRequestDurationOptions>(host, DefaultRuleName));

                Assert.Equal(string.Format(OptionsDisplayStrings.ErrorMessage_NoDefaultRequestCount), invalidOptionsException.Message);
            });
        }

        [Fact]
        public async Task DefaultRequestCount_RequestDuration_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetRequestCount(TriggerTestsConstants.UnknownRequestCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetRequestDurationOptions options = new AspNetRequestDurationOptions();
                options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestDuration")
                    .Trigger.Settings = options;
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
                rootOptions.CreateCollectionRuleDefaults().SetResponseCount(TriggerTestsConstants.ExpectedResponseCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetResponseStatusOptions options = new AspNetResponseStatusOptions(); // Omit setting the Response Count
                options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetResponseStatus")
                    .Trigger.Settings = options; // Can we push this out to reading from a field?
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

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetResponseStatusOptions options = new AspNetResponseStatusOptions();
                options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetResponseStatus")
                    .Trigger.Settings = options; // Omit Response Count and don't set a default
            }, host =>
            {
                OptionsValidationException invalidOptionsException = Assert.Throws<OptionsValidationException>(
                        () => TriggerTestsHelper.GetTriggerOptions<AspNetResponseStatusOptions>(host, DefaultRuleName));

                Assert.Equal(string.Format(OptionsDisplayStrings.ErrorMessage_NoDefaultResponseCount), invalidOptionsException.Message);
            });
        }

        [Fact]
        public async Task DefaultResponseCount_Override()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetResponseCount(TriggerTestsConstants.UnknownResponseCount);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetResponseStatusOptions options = new AspNetResponseStatusOptions();
                options.ResponseCount = TriggerTestsConstants.ExpectedResponseCount;
                options.StatusCodes = TriggerTestsConstants.ExpectedStatusCodes;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetResponseStatus")
                    .Trigger.Settings = options;
            }, host =>
            {
                AspNetResponseStatusOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetResponseStatusOptions>(host, DefaultRuleName);

                Assert.Equal(TriggerTestsConstants.ExpectedResponseCount, options.ResponseCount);
            });
        }

        [Fact]
        public async Task DefaultSlidingWindowDuration_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRuleDefaults().SetSlidingWindowDuration(TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration));

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetRequestCountOptions options = new AspNetRequestCountOptions();
                options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestCount")
                    .Trigger.Settings = options;
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
                rootOptions.CreateCollectionRuleDefaults().SetSlidingWindowDuration(TimeSpan.Parse(TriggerTestsConstants.UnknownSlidingWindowDuration));

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                AspNetRequestCountOptions options = new AspNetRequestCountOptions();
                options.RequestCount = TriggerTestsConstants.ExpectedRequestCount;
                options.SlidingWindowDuration = TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider)
                    .SetTrigger("AspNetRequestCount")
                    .Trigger.Settings = options;
            }, host =>
            {
                AspNetRequestCountOptions options = TriggerTestsHelper.GetTriggerOptions<AspNetRequestCountOptions>(host, DefaultRuleName);

                Assert.Equal(TimeSpan.Parse(TriggerTestsConstants.ExpectedSlidingWindowDuration), options.SlidingWindowDuration);
            });
        }
    }
}
