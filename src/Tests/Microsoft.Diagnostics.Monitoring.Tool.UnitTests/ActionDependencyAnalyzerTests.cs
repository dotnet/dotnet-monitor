// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ActionDependencyAnalyzerTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private sealed class TestProcessInfo : WebApi.IProcessInfo
        {
            public TestProcessInfo(Guid runtimeInstanceCookie, int processId = 0, string commandLine = null, string operatingSystem = null, string processArchitecture = null)
            {
                EndpointInfo = new TestEndpointInfo(runtimeInstanceCookie, processId, commandLine, operatingSystem, processArchitecture);
            }

            public IEndpointInfo EndpointInfo { get; }

            public string CommandLine => EndpointInfo.CommandLine;

            public string OperatingSystem => EndpointInfo.OperatingSystem;

            public string ProcessArchitecture => EndpointInfo.ProcessArchitecture;

            public string ProcessName => ProcessInfoImpl.GetProcessName(CommandLine, OperatingSystem);
        }

        private sealed class TestEndpointInfo : EndpointInfoBase
        {
            public TestEndpointInfo(Guid runtimeInstanceCookie, int processId, string commandLine, string operatingSystem, string processArchitecture)
            {
                ProcessId = processId;
                RuntimeInstanceCookie = runtimeInstanceCookie;
                CommandLine = commandLine;
                OperatingSystem = operatingSystem;
                ProcessArchitecture = processArchitecture;
                ServiceProvider = new NotSupportedServiceProvider();
            }

            public override int ProcessId { get; protected set; }
            public override Guid RuntimeInstanceCookie { get; protected set; }
            public override string CommandLine { get; protected set; }
            public override string OperatingSystem { get; protected set; }
            public override string ProcessArchitecture { get; protected set; }

            public override Version RuntimeVersion { get; protected set; }
            public override IServiceProvider ServiceProvider { get; protected set; }
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
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                Guid instanceId = Guid.NewGuid();
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId), HostInfo.GetCurrent(timeProvider), logger);

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
        public async Task ProcessInfoTest()
        {
            PassThroughOptions settings = null;
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", ConfigurationTokenParser.ProcessNameReference, ConfigurationTokenParser.ProcessIdReference, ConfigurationTokenParser.CommandLineReference)
                    .SetStartupTrigger();

                settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, host =>
            {
                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                const string processName = "actionProcess";
                const int processId = 123;
                string commandLine = FormattableString.Invariant($"{processName} arg1");

                Guid instanceId = Guid.NewGuid();
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId, processId: processId, commandLine: commandLine), HostInfo.GetCurrent(timeProvider), logger);

                ActionOptionsDependencyAnalyzer analyzer = ActionOptionsDependencyAnalyzer.Create(context);
                PassThroughOptions newSettings = (PassThroughOptions)analyzer.SubstituteOptionValues(new Dictionary<string, CollectionRuleActionResult>(), 1, settings);

                Assert.Equal(processName, newSettings.Input1);
                Assert.Equal(processId.ToString(CultureInfo.InvariantCulture), newSettings.Input2);
                Assert.Equal(commandLine, newSettings.Input3);

            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }

        [Fact]
        public async Task HostInfoTest()
        {
            PassThroughOptions settings = null;
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", ConfigurationTokenParser.HostNameReference, ConfigurationTokenParser.UnixTimeReference, "test")
                    .SetStartupTrigger();

                settings = (PassThroughOptions)options.Actions.Last().Settings;
            }, host =>
            {
                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                MockTimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>() as MockTimeProvider;

                const string hostName = "exampleHost";
                Guid instanceId = Guid.NewGuid();
                HostInfo hostInfo = new HostInfo(hostName, timeProvider);
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId), hostInfo, logger);

                ActionOptionsDependencyAnalyzer analyzer = ActionOptionsDependencyAnalyzer.Create(context);
                PassThroughOptions newSettings = (PassThroughOptions)analyzer.SubstituteOptionValues(new Dictionary<string, CollectionRuleActionResult>(), 1, settings);

                Assert.Equal(hostName, newSettings.Input1);
                Assert.Equal(hostInfo.TimeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), newSettings.Input2);

            }, serviceCollection =>
            {
                serviceCollection.AddSingleton<TimeProvider, MockTimeProvider>();
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
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                Guid instanceId = Guid.NewGuid();
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId), HostInfo.GetCurrent(timeProvider), logger);

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
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                Guid instanceId = Guid.NewGuid();
                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId), HostInfo.GetCurrent(timeProvider), logger);

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
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                Guid instanceId = Guid.NewGuid();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, new TestProcessInfo(instanceId), HostInfo.GetCurrent(timeProvider), logger);

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
                Assert.Equal(FormattableString.Invariant($"a1input1 with rid: {instanceId:D} and a1input2"), a2output1);
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
