// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Microsoft.Diagnostics.Monitoring.Extension.Common;
using System.CommandLine;

namespace Microsoft.Diagnostics.Monitoring.AzureEventHubsStorage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Expected command line format is: dotnet-monitor-egress-azureblobstorage.exe Egress
            CliRootCommand rootCommand = new CliRootCommand("Egresses an artifact to Azure Event Hubs.");

            CliCommand egressCmd = null; // EgressHelper.CreateEgressCommand<AzureBlobEgressProvider, AzureBlobEgressProviderOptions>(ConfigureServices);

            rootCommand.Add(egressCmd);

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
