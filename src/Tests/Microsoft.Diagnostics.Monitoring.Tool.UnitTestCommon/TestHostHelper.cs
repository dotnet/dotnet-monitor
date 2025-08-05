// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class TestHostHelper
    {
        public static async Task CreateCollectionRulesHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup,
            Func<IHost, Task> hostCallback,
            Action<IServiceCollection> servicesCallback = null,
            Action<ILoggingBuilder> loggingCallback = null,
            List<IConfigurationSource> overrideSource = null)
        {
            IHost host = CreateHost(outputHelper, setup, servicesCallback, loggingCallback, overrideSource);
            try
            {
                //It is necessary to start the host so that the OperationsStore background service is started.
                await host.StartAsync();
                await hostCallback(host);
            }
            finally
            {
                await host.StopAsync();
                await DisposableHelper.DisposeAsync(host);
            }
        }

        public static async Task CreateCollectionRulesHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup,
            Action<IHost> hostCallback,
            Action<IServiceCollection> servicesCallback = null,
            Action<ILoggingBuilder> loggingCallback = null,
            List<IConfigurationSource> overrideSource = null)
        {
            IHost host = CreateHost(outputHelper, setup, servicesCallback, loggingCallback, overrideSource);

            try
            {
                hostCallback(host);
            }
            finally
            {
                await DisposableHelper.DisposeAsync(host);
            }
        }

        public static IHost CreateHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup,
            Action<IServiceCollection> servicesCallback,
            Action<ILoggingBuilder> loggingCallback = null,
            List<IConfigurationSource> overrideSource = null,
            HostBuilderSettings settings = null)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    RootOptions options = new();
                    setup(options);

                    CommonOptionsMapper optionsMapper = new();
                    optionsMapper.AddActionSettings<PassThroughOptions>(nameof(PassThroughAction), MapPassThroughOptions);
                    IDictionary<string, string> configurationValues = optionsMapper.ToConfigurationValues(options);
                    outputHelper.WriteLine("Begin Configuration:");
                    foreach ((string key, string value) in configurationValues)
                    {
                        outputHelper.WriteLine("{0} = {1}", key, value);
                    }
                    outputHelper.WriteLine("End Configuration");

                    builder.AddInMemoryCollection(configurationValues);

                    overrideSource?.ForEach(source => builder.Sources.Add(source));
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingCallback?.Invoke(loggingBuilder);

                    loggingBuilder.Services.AddSingleton<ILoggerProvider, TestOutputLoggerProvider>();
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    services.AddSingleton<ITestOutputHelper>(outputHelper);
                    services.AddSingleton(TimeProvider.System);
                    services.ConfigureGlobalCounter(context.Configuration);
                    services.ConfigureCollectionRuleDefaults(context.Configuration);
                    services.ConfigureTemplates(context.Configuration);
                    services.ConfigureDotnetMonitorDebug(context.Configuration);
                    services.AddSingleton<OperationTrackerService>();
                    services.ConfigureCollectionRules();

                    services.ConfigureExtensions();
                    if (settings != null)
                    {
                        services.AddSingleton<IDotnetToolsFileSystem, TestDotnetToolsFileSystem>();
                        services.ConfigureExtensionLocations(settings);
                    }

                    services.ConfigureEgress();
                    services.ConfigureRequestLimits();
                    services.ConfigureOperationStore();

                    services.ConfigureDiagnosticPort(context.Configuration);

                    services.AddSingleton<IDumpService, DumpService>();
                    services.ConfigureStorage(context.Configuration);
                    services.ConfigureInProcessFeatures(context.Configuration);
                    services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                    services.AddSingleton<IDumpOperationFactory, DumpOperationFactory>();
                    services.AddSingleton<ILogsOperationFactory, LogsOperationFactory>();
                    services.AddSingleton<IMetricsOperationFactory, MetricsOperationFactory>();
                    services.AddSingleton<ITraceOperationFactory, TraceOperationFactory>();
                    services.AddSingleton<IGCDumpOperationFactory, GCDumpOperationFactory>();
                    servicesCallback?.Invoke(services);
                })
                .Build();
        }

        private static void MapPassThroughOptions(PassThroughOptions obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Input1, FormattableString.Invariant($"{prefix}{nameof(obj.Input1)}"));
                MapString(obj.Input2, FormattableString.Invariant($"{prefix}{nameof(obj.Input2)}"));
                MapString(obj.Input3, FormattableString.Invariant($"{prefix}{nameof(obj.Input3)}"));
            }

            void MapString(string value, string valueName)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }
    }
}
