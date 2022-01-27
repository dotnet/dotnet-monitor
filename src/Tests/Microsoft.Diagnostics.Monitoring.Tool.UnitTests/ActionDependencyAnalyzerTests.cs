// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly ITestOutputHelper _outputHelper;
        private static readonly TimeSpan TimeoutMs = TimeSpan.FromMilliseconds(500);
        private const string DefaultRuleName = nameof(ActionDependencyAnalyzerTests);

        public ActionDependencyAnalyzerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
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
    }
}
