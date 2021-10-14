// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Threading;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Xunit.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions;
using System.Linq;


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

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                ActionOptionsDependencyAnalyzer analyzer = new ActionOptionsDependencyAnalyzer(context);
                analyzer.GetActionDependencies(1);
                analyzer.SubstituteOptionValues(1, settings);

                Assert.Equal(3, record.Events.Count);
                Assert.Equal(LoggingEventIds.InvalidActionReferenceToken, record.Events[0].EventId.Id);
                Assert.Equal(LoggingEventIds.InvalidActionReference, record.Events[1].EventId.Id);
                Assert.Equal(LoggingEventIds.InvalidActionResultReference, record.Events[2].EventId.Id);

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
