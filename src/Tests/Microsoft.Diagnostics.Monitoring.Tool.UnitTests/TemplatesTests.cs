// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class TemplatesTests
    {
        private readonly ITestOutputHelper _outputHelper;

        private const string SampleConfigurationsDirectory = "TemplatesConfigurations";

        // This should be identical to the error message found in Strings.resx
        private string TemplateNotFoundErrorMessage = "Could not find a template with the name: {0}";

        public TemplatesTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that Templates are correctly translated from JSON to CollectionRuleOptions.
        /// </summary>
        [Fact]
        public async void TemplatesTranslationSuccessTest()
        {
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions => { }, host =>
            {
                IOptionsMonitor<CollectionRuleOptions> optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();

                CollectionRuleOptions options = optionsMonitor.Get("ValidRule");

                // Trigger Comparison
                Assert.Equal(KnownCollectionRuleTriggers.AspNetRequestCount, options.Trigger.Type);
                Assert.Equal(20, ((AspNetRequestCountOptions)options.Trigger.Settings).RequestCount);
                Assert.Equal(TimeSpan.Parse("00:01:00"), ((AspNetRequestCountOptions)options.Trigger.Settings).SlidingWindowDuration);

                // Actions Comparison
                Assert.Equal(3, options.Actions.Count);
                Assert.Equal(KnownCollectionRuleActions.CollectGCDump, options.Actions[0].Type);
                Assert.Equal("artifacts", ((CollectGCDumpOptions)options.Actions[0].Settings).Egress);
                Assert.Equal(KnownCollectionRuleActions.CollectGCDump, options.Actions[1].Type);
                Assert.Equal("artifacts2", ((CollectGCDumpOptions)options.Actions[1].Settings).Egress);
                Assert.Equal(KnownCollectionRuleActions.CollectTrace, options.Actions[2].Type);
                Assert.Equal("artifacts2", ((CollectTraceOptions)options.Actions[2].Settings).Egress);
                Assert.Equal(WebApi.Models.TraceProfile.Cpu, ((CollectTraceOptions)options.Actions[2].Settings).Profile);

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

            }, overrideSource: GetConfigurationSources());
        }

        /// <summary>
        /// Tests that incorrectly referenced Templates error correctly.
        /// </summary>
        [Fact]
        public async void TemplatesTranslationFailTest()
        {
            using TemporaryDirectory userConfigDir = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions => { }, host =>
            {
                IOptionsMonitor<CollectionRuleOptions> optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();

                OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => optionsMonitor.Get("InvalidRule"));

                string[] failures = ex.Failures.ToArray();
                Assert.Equal(2, failures.Length);

                Assert.Equal(string.Format(TemplateNotFoundErrorMessage, "TriggerTemplateINVALID"), failures[0]);
                Assert.Equal(string.Format(TemplateNotFoundErrorMessage, "FilterTemplateINVALID"), failures[1]);

            }, overrideSource: GetConfigurationSources());
        }

        private static List<IConfigurationSource> GetConfigurationSources()
        {
            string[] filePaths = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), SampleConfigurationsDirectory));

            List<IConfigurationSource> sources = new();

            foreach (string filePath in filePaths)
            {
                JsonConfigurationSource source = new JsonConfigurationSource();
                source.Path = filePath;
                source.ResolveFileProvider();
                sources.Add(source);
            }

            return sources;
        }
    }
}
