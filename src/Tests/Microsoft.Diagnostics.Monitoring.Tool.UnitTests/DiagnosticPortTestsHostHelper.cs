// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class DiagnosticPortTestsHostHelper
    {
        public static async Task CreateDiagnosticPortHost(
            ITestOutputHelper outputHelper,
            Action<IHost> hostCallback,
            IDictionary<string, string> diagnosticPortEnvironmentVariables)
        {
            IHost host = CreateHost(outputHelper, diagnosticPortEnvironmentVariables);

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
            IDictionary<string, string> diagnosticPortEnvironmentVariables)
        {
            IHostBuilder hostBuilder = DiagnosticPortTestsHelper.GetDiagnosticPortHostBuilder(outputHelper, diagnosticPortEnvironmentVariables);

            return hostBuilder
                .ConfigureAppConfiguration(builder =>
                {
                    builder.ConfigureStorageDefaults();
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    services.AddSingleton<ITestOutputHelper>(outputHelper);

                    services.Configure<DiagnosticPortOptions>(context.Configuration.GetSection(ConfigurationKeys.DiagnosticPort));
                    services.AddSingleton<IPostConfigureOptions<DiagnosticPortOptions>, DiagnosticPortPostConfigureOptions>();
                    services.AddSingleton<IValidateOptions<DiagnosticPortOptions>, DiagnosticPortValidateOptions>();
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
