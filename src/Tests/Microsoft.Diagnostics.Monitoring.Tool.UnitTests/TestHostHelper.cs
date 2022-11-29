﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
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
                await hostCallback(host);
            }
            finally
            {
                await DisposeHost(host);
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
                await DisposeHost(host);
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

                    IDictionary<string, string> configurationValues = options.ToConfigurationValues();
                    outputHelper.WriteLine("Begin Configuration:");
                    foreach ((string key, string value) in configurationValues)
                    {
                        outputHelper.WriteLine("{0} = {1}", key, value);
                    }
                    outputHelper.WriteLine("End Configuration");

                    builder.AddInMemoryCollection(configurationValues);

                    if (null != overrideSource)
                    {
                        overrideSource.ForEach(source => builder.Sources.Add(source));
                    }
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingCallback?.Invoke(loggingBuilder);

                    loggingBuilder.Services.AddSingleton<ILoggerProvider, TestOutputLoggerProvider>();
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    services.AddSingleton<ITestOutputHelper>(outputHelper);
                    services.AddSingleton(RealSystemClock.Instance);
                    services.ConfigureGlobalCounter(context.Configuration);
                    services.ConfigureCollectionRuleDefaults(context.Configuration);
                    services.ConfigureTemplates(context.Configuration);
                    services.AddSingleton<OperationTrackerService>();
                    services.ConfigureCollectionRules();

                    if (settings != null)
                    {
                        services.ConfigureExtensions(settings);
                    }

                    services.ConfigureEgress();

                    services.ConfigureDiagnosticPort(context.Configuration);

                    services.AddSingleton<IDumpService, DumpService>();
                    services.ConfigureStorage(context.Configuration);
                    services.ConfigureInProcessFeatures(context.Configuration);
                    services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                    servicesCallback?.Invoke(services);
                })
                .Build();
        }

        public static async Task DisposeHost(IHost host)
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                host.Dispose();
            }
        }
    }
}
