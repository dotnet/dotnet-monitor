// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.CommandLine;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Expected command line format is: dotnet-monitor-egress-azureblobstorage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to Azure storage.");

            Command egressCmd = EgressHelper.CreateEgressCommand<AzureBlobEgressProvider, AzureBlobEgressProviderOptions>(ConfigureServices);

            rootCommand.Add(egressCmd);

            return await rootCommand.Parse(args).InvokeAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPostConfigureOptions<AzureBlobEgressProviderOptions>, PostConfigureAzureBlobEgressProviderOptions>();
        }
    }
}
