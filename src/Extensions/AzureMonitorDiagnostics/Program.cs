// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

internal sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        ILogger logger = Utilities.CreateLogger();
        AzureMonitorDiagnosticsEgressProvider provider = new(logger);

        // Expected command line format is: dotnet-monitor-egress-azuremonitordiagnostics.exe Egress
        RootCommand rootCommand = new("Uploads an artifact to Azure Monitor Diagnostic Services.");
        Command egressCmd = EgressHelper.CreateEgressCommand(provider, ConfigureOptions);
        rootCommand.Add(egressCmd);
        return await rootCommand.InvokeAsync(args);

        static void ConfigureOptions(ExtensionEgressPayload configPayload, AzureMonitorDiagnosticsEgressProviderOptions options)
        {
            Validator.ValidateObject(options, new ValidationContext(options));
        }
    }
}
