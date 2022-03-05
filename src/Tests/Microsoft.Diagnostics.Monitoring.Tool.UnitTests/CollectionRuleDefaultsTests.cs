﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Options;
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

                Assert.Equal(string.Format(Tools.Monitor.Strings.ErrorMessage_NoDefaultEgressProvider), invalidOptionsException.Message);
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
    }
}
