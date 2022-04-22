// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CustomShortcutsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        private const string SampleConfigurationsDirectory = "CustomShortcutsConfigurations";

        public CustomShortcutsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that Custom Shortcuts are correctly translated from JSON to CollectionRuleOptions.
        /// </summary>
        [Fact]
        public async void CustomShortcutsTranslationSuccessTest()
        {
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            IHostBuilder builder = GetHostBuilder(userConfigDir);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions => {}, host =>
            {
                IOptionsMonitor<CollectionRuleOptions> optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();

                CollectionRuleOptions options = optionsMonitor.Get("ValidRule");

                // Trigger Comparison
                Assert.Equal(KnownCollectionRuleTriggers.AspNetRequestCount, options.Trigger.Type);
                Assert.Equal(20, ((AspNetRequestCountOptions)options.Trigger.Settings).RequestCount);
                Assert.Equal(TimeSpan.Parse("00:01:00"), ((AspNetRequestCountOptions)options.Trigger.Settings).SlidingWindowDuration);

                // Actions Comparison
                Assert.Equal(4, options.Actions.Count);
                Assert.Equal(KnownCollectionRuleActions.CollectGCDump, options.Actions[0].Type);
                Assert.Equal("artifacts", ((CollectGCDumpOptions)options.Actions[0].Settings).Egress);
                Assert.Equal(KnownCollectionRuleActions.CollectGCDump, options.Actions[1].Type);
                Assert.Equal("artifacts2", ((CollectGCDumpOptions)options.Actions[1].Settings).Egress);
                Assert.Equal(KnownCollectionRuleActions.CollectTrace, options.Actions[2].Type);
                Assert.Equal("monitorBlob", ((CollectTraceOptions)options.Actions[2].Settings).Egress);
                Assert.Equal(WebApi.Models.TraceProfile.Cpu, ((CollectTraceOptions)options.Actions[2].Settings).Profile);
                Assert.Equal(KnownCollectionRuleActions.CollectDump, options.Actions[3].Type);
                Assert.Equal("monitorBlob", ((CollectDumpOptions)options.Actions[3].Settings).Egress);

                // Filters Comparison
                Assert.Equal(2, options.Filters.Count);
                Assert.Equal(WebApi.ProcessFilterKey.ProcessName, options.Filters[0].Key);
                Assert.Equal("FirstWebApp", options.Filters[0].Value);
                Assert.Equal(WebApi.ProcessFilterKey.ProcessName, options.Filters[1].Key);
                Assert.Equal("FirstWebApp1", options.Filters[1].Value);
                Assert.Equal(WebApi.ProcessFilterType.Exact, options.Filters[1].MatchType);

                // Limits Comparison
                Assert.Equal(1, options.Limits.ActionCount);
                Assert.Equal(TimeSpan.Parse("00:00:30"), options.Limits.ActionCountSlidingWindowDuration);
                Assert.Equal(TimeSpan.Parse("00:05:00"), options.Limits.RuleDuration);

            }, builder: (HostBuilder)builder);
        }

        /// <summary>
        /// Tests that incorrectly referenced Custom Shortcuts cause options to be nullifed.
        /// </summary>
        [Fact]
        public async void CustomShortcutsTranslationFailTest()
        {
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            IHostBuilder builder = GetHostBuilder(userConfigDir);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions => { }, host =>
            {
                IOptionsMonitor<CollectionRuleOptions> optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();

                OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => optionsMonitor.Get("InvalidRule"));

                Assert.Equal(string.Format("The Trigger field is required."), ex.Message); // Push to resx?

            }, builder: (HostBuilder)builder);
        }

        private IHostBuilder GetHostBuilder(TemporaryDirectory userConfigDir)
        {
            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                Authentication = HostBuilderHelper.CreateAuthConfiguration(noAuth: false, tempApiKey: false),
                ContentRootDirectory = string.Empty,
                SharedConfigDirectory = string.Empty,
                UserConfigDirectory = userConfigDir.FullName
            };

            // This is the settings.json file in the user profile directory.
            File.WriteAllText(Path.Combine(userConfigDir.FullName, "settings.json"), ConfigurationTests.ConstructSettingsJson(SampleConfigurationsDirectory));

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment();
            builder.ReplaceDotnetEnvironment();
            builder.ReplaceMonitorEnvironment();

            return builder;
        }
    }
}
