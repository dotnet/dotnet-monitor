﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionDependencyAnalyzerTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private sealed class TestEndpointInfo : WebApi.EndpointInfoBase
        {
            public TestEndpointInfo(Guid runtimeInstanceCookie, int processId = 0, string commandLine = null, string operatingSystem = null, string processArchitecture = null)
            {
                ProcessId = processId;
                RuntimeInstanceCookie = runtimeInstanceCookie;
                CommandLine = commandLine;
                OperatingSystem = operatingSystem;
                ProcessArchitecture = processArchitecture;
            }

            public override int ProcessId { get; protected set; }
            public override Guid RuntimeInstanceCookie { get; protected set; }
            public override string CommandLine { get; protected set; }
            public override string OperatingSystem { get; protected set; }
            public override string ProcessArchitecture { get; protected set; }
        }

        private readonly ITestOutputHelper _outputHelper;
        private static readonly TimeSpan TimeoutMs = TimeSpan.FromMilliseconds(500);
        private const string DefaultRuleName = nameof(ActionDependencyAnalyzerTests);

        public ActionDependencyAnalyzerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task DependenciesTest()
        {
            const string Output1 = nameof(Output1);
            const string Output2 = nameof(Output2);
            const string Output3 = nameof(Output3);

            string a2input1 = FormattableString.Invariant($"$(Actions.a1.{Output1}) with $(Actions.a1.{Output2})T");
            string a2input2 = FormattableString.Invariant($"$(Actions.a1.{Output2})");
            string a2input3 = FormattableString.Invariant($"Output $(Actions.a1.{Output3}) trail");

            PassThroughOptions a2Settings = null;

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3")
                    .AddPassThroughAction("a2", a2input1, a2input2, a2input3)
                    .SetStartupTrigger();

                a2Settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                ISystemClock clock = host.Services.GetRequiredService<ISystemClock>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger, clock);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                IDictionary<string, CollectionRuleActionResult> results = await executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token);

                //Verify that the original settings were not altered during execution.
                Assert.Equal(a2input1, a2Settings.Input1);
                Assert.Equal(a2input2, a2Settings.Input2);
                Assert.Equal(a2input3, a2Settings.Input3);

                Assert.Equal(1, callbackCount);
                Assert.Equal(2, results.Count);
                Assert.True(results.TryGetValue("a2", out CollectionRuleActionResult a2result));
                Assert.Equal(3, a2result.OutputValues.Count);

                Assert.True(a2result.OutputValues.TryGetValue(Output1, out string a2output1));
                Assert.Equal("a1input1 with a1input2T", a2output1);
                Assert.True(a2result.OutputValues.TryGetValue(Output2, out string a2output2));
                Assert.Equal("a1input2", a2output2);
                Assert.True(a2result.OutputValues.TryGetValue(Output3, out string a2output3));
                Assert.Equal("Output a1input3 trail", a2output3);
            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }

        [Fact]
        public async Task InvalidTokenReferenceTest()
        {
            string a2input1 = "$(Actions. badly formed";
            string a2input2 = "$(Actions.a15.MissingAction)";
            string a2input3 = "$(Actions.a1.MissingResult)";

            LogRecord record = new LogRecord();
            PassThroughOptions settings = null;
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3")
                    .AddPassThroughAction("a2", a2input1, a2input2, a2input3)
                    .SetStartupTrigger();

                settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, host =>
            {
                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                ISystemClock clock = host.Services.GetRequiredService<ISystemClock>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger, clock);

                ActionOptionsDependencyAnalyzer analyzer = ActionOptionsDependencyAnalyzer.Create(context);
                analyzer.GetActionDependencies(1);
                analyzer.SubstituteOptionValues(new Dictionary<string, CollectionRuleActionResult>(), 1, settings);

                Assert.Equal(3, record.Events.Count);
                Assert.Equal(LoggingEventIds.InvalidActionReferenceToken.Id(), record.Events[0].EventId.Id);
                Assert.Equal(LoggingEventIds.InvalidActionReference.Id(), record.Events[1].EventId.Id);
                Assert.Equal(LoggingEventIds.InvalidActionResultReference.Id(), record.Events[2].EventId.Id);

            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            }, loggingBuilder =>
            {
                loggingBuilder.AddProvider(new TestLoggerProvider(record));
            });
        }

        [Fact]
        public async Task RuntimeIdReferenceTest()
        {
            PassThroughOptions settings = null;
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", ConfigurationTokenParser.RuntimeIdReference, "test", "test")
                    .SetStartupTrigger();

                settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, host =>
            {
                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                ISystemClock clock = host.Services.GetRequiredService<ISystemClock>();

                Guid instanceId = Guid.NewGuid();
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestEndpointInfo(instanceId), logger, clock);

                ActionOptionsDependencyAnalyzer analyzer = ActionOptionsDependencyAnalyzer.Create(context);
                PassThroughOptions newSettings = (PassThroughOptions)analyzer.SubstituteOptionValues(new Dictionary<string, CollectionRuleActionResult>(), 1, settings);

                Assert.Equal(instanceId.ToString("D"), newSettings.Input1);

            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }

        [Fact]
        public async Task ReplacementsAndDependencies()
        {
            const string Output1 = nameof(Output1);
            const string Output2 = nameof(Output2);
            const string Output3 = nameof(Output3);

            string a2input1 = FormattableString.Invariant($"$(Actions.a1.{Output1}) with rid: $(Process.RuntimeId) and $(Actions.a1.{Output2})");
            string a2input2 = FormattableString.Invariant($"$(Actions.a1.{Output2})");
            string a2input3 = FormattableString.Invariant($"Output $(Actions.a1.{Output3}) trail");

            PassThroughOptions a2Settings = null;

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3")
                    .AddPassThroughAction("a2", a2input1, a2input2, a2input3)
                    .SetStartupTrigger();

                a2Settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                ISystemClock clock = host.Services.GetRequiredService<ISystemClock>();

                Guid instanceId = Guid.NewGuid();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestEndpointInfo(instanceId), logger, clock);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                IDictionary<string, CollectionRuleActionResult> results = await executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token);

                //Verify that the original settings were not altered during execution.
                Assert.Equal(a2input1, a2Settings.Input1);
                Assert.Equal(a2input2, a2Settings.Input2);
                Assert.Equal(a2input3, a2Settings.Input3);

                Assert.Equal(1, callbackCount);
                Assert.Equal(2, results.Count);
                Assert.True(results.TryGetValue("a2", out CollectionRuleActionResult a2result));
                Assert.Equal(3, a2result.OutputValues.Count);

                Assert.True(a2result.OutputValues.TryGetValue(Output1, out string a2output1));
                Assert.Equal(FormattableString.Invariant($"a1input1 with rid: {instanceId.ToString("D")} and a1input2"), a2output1);
                Assert.True(a2result.OutputValues.TryGetValue(Output2, out string a2output2));
                Assert.Equal("a1input2", a2output2);
                Assert.True(a2result.OutputValues.TryGetValue(Output3, out string a2output3));
                Assert.Equal("Output a1input3 trail", a2output3);
            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }
    }
}
