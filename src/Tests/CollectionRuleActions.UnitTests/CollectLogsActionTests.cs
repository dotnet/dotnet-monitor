// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using DisposableHelper = Microsoft.Diagnostics.Monitoring.TestCommon.DisposableHelper;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public class CollectLogsActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const string DefaultRuleName = "LogsTestRule";

        public CollectLogsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        /// <summary>
        /// Test that log events with a LogsTestUtilities.Category that doesn't have a specified level are collected
        /// at the log level specified in the request body.
        /// </summary>
        /// 
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4741")]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndLogFormat), MemberType = typeof(ActionTestsHelper))]
        public Task LogsDefaultLevelFallbackActionTest(TargetFrameworkMoniker tfm, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { TestAppScenarios.Logger.Categories.LoggerCategory1, LogLevel.Error },
                        { TestAppScenarios.Logger.Categories.LoggerCategory2, null },
                        { TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Warning }
                    },
                    LogLevel = LogLevel.Information,
                    UseAppFilters = false
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat,
                tfm);
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4741")]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndLogFormat), MemberType = typeof(ActionTestsHelper))]
        public Task LogsUseAppFiltersViaBodyActionTest(TargetFrameworkMoniker tfm, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                new LogsConfiguration()
                {
                    LogLevel = LogLevel.Trace,
                    UseAppFilters = true
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat,
                tfm);
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application
        /// and for the categories and levels specified in the filter specs.
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4741")]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndLogFormat), MemberType = typeof(ActionTestsHelper))]
        public Task LogsUseAppFiltersAndFilterSpecsActionTest(TargetFrameworkMoniker tfm, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Debug }
                    },
                    LogLevel = LogLevel.Trace,
                    UseAppFilters = true
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat,
                tfm);
        }

        /// <summary>
        /// Test that log events are collected for wildcard categories.
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4741")]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndLogFormat), MemberType = typeof(ActionTestsHelper))]
        public Task LogsWildcardActionTest(TargetFrameworkMoniker tfm, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { "*", LogLevel.Trace },
                        { TestAppScenarios.Logger.Categories.LoggerCategory2, LogLevel.Warning }
                    },
                    LogLevel = LogLevel.Information,
                    UseAppFilters = false
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat,
                tfm);
        }

        private async Task ValidateLogsActionAsync(
            LogsConfiguration configuration,
            Func<ChannelReader<LogEntry>, Task> callback,
            LogFormat logFormat,
            TargetFrameworkMoniker tfm)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            Pipe logEntriesPipe = new();

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLogsAction(ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = CommonTestTimeouts.LogsDuration;
                        options.FilterSpecs = configuration.FilterSpecs;
                        options.DefaultLevel = configuration.LogLevel;
                        options.Format = logFormat;
                        options.UseAppFilters = configuration.UseAppFilters;
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectLogsOptions options = ActionTestsHelper.GetActionOptions<CollectLogsOptions>(host, DefaultRuleName);

                // This is reassigned here due to a quirk in which a null value in the dictionary has its key removed, thus causing LogsDefaultLevelFallbackActionTest to fail. By reassigning here, any keys with null values are maintained.
                options.FilterSpecs = configuration.FilterSpecs;

                // The Sentinel and Flush categories are implementation details of the logs test infrastructure.
                // Instead of having each test understand to add these, add them to the configuration
                // if the test is using FilterSpecs.
                if (null != options.FilterSpecs)
                {
                    options.FilterSpecs.Add(TestAppScenarios.Logger.Categories.SentinelCategory, LogLevel.Critical);
                    options.FilterSpecs.Add(TestAppScenarios.Logger.Categories.FlushCategory, LogLevel.Critical);
                }

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLogs, out factory));

                EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(endpointInfoCallback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                runner.ScenarioName = TestAppScenarios.Logger.Name;

                Task<IProcessInfo> processInfoTask = endpointInfoCallback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IProcessInfo processInfo = await processInfoTask;

                    ICollectionRuleAction action = factory.Create(processInfo, options);

                    using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.LogsTimeout);

                    try
                    {
                        await action.StartAsync(cancellationTokenSource.Token);

                        await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);

                        using Stream readerStream = logEntriesPipe.Reader.AsStream();

                        await LogsTestUtilities.ValidateLogsEquality(readerStream, callback, logFormat, _outputHelper);

                        // Note: Do not wait for completion of the action execution. No more relevant data will be produced;
                        // the code would only be waiting for the action to end. Ideally the action is gracefully stopped at
                        // this point.
                    }
                    finally
                    {
                        await DisposableHelper.DisposeAsync(action);
                    }
                });
            },
            services =>
            {
                services.AddSingleton<IWellKnownExtensionFactory>(sp => new PipeEgressExtensionFactory(logEntriesPipe.Writer));
            },
            overrideSource: new List<IConfigurationSource>()
            {
                new MemoryConfigurationSource()
                {
                    InitialData = new Dictionary<string, string>()
                    {
                        { ConfigurationPath.Combine(ConfigurationKeys.Egress, PipeEgressExtension.Name, ActionTestsConstants.ExpectedEgressProvider), "UnusedOptions" }
                    }
                }
            });
        }

        public static bool SkipOnWindowsNetCore31
        {
            get
            {
                // Skip logs tests for .NET Core 3.1 on Windows; these tests sporadically
                // fail frequently causing insertions and builds with unrelated changes to
                // fail. See https://github.com/dotnet/dotnet-monitor/issues/807 for details.
                return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    TestDotNetHost.RuntimeVersion.Major != 3 ||
                    TestDotNetHost.RuntimeVersion.Minor != 1;
            }
        }
    }
}
