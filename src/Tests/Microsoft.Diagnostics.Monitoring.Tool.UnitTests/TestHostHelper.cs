using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class TestHostHelper
    {
        public static async Task CreateCollectionRulesHost(
            ITestOutputHelper outputHelper,
            Action<RootOptions> setup,
            Action<IHost> callback)
        {
            IHost host = new HostBuilder()
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
                })
                .ConfigureServices(services =>
                {
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();
                })
                .Build();

            try
            {
                callback(host);
            }
            finally
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
}
