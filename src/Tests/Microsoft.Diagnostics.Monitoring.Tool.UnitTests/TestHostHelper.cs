// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            Func<IHost, Task> callback)
        {
            IHost host = CreateHost(outputHelper, setup);

            try
            {
                await callback(host);
            }
            finally
            {
                await DisposeHost(host);
            }
        }

        public static async Task CreateCollectionRulesHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup,
            Action<IHost> callback)
        {
            IHost host = CreateHost(outputHelper, setup);

            try
            {
                callback(host);
            }
            finally
            {
                await DisposeHost(host);
            }
        }

        public static IHost CreateHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup)
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

                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {ConfigurationPath.Combine(ConfigurationKeys.Storage, nameof(StorageOptions.DumpTempFolder)), StorageOptionsDefaults.DumpTempFolder }
                    });
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();

                    services.AddSingleton<EgressOperationQueue>();
                    services.AddSingleton<EgressOperationStore>();
                    services.AddSingleton<RequestLimitTracker>();
                    services.AddSingleton<WebApi.IEndpointInfoSource, FilteredEndpointInfoSource>();
                    services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                    services.ConfigureStorage(context.Configuration);
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
