// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            ILogger logger = Utilities.CreateLogger();

            S3StorageEgressProvider provider = new(logger);

            // Expected command line format is: dotnet-monitor-egress-s3storage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to S3 storage.");

            // TODO: Not currently doing any extra configuration/validation for S3 here
            Command egressCmd = EgressHelper.CreateEgressCommand(provider);

            rootCommand.Add(egressCmd);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
