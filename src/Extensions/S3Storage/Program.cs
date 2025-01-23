// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Expected command line format is: dotnet-monitor-egress-s3storage.exe Egress
            RootCommand rootCommand = new RootCommand("Egresses an artifact to S3 storage.");

            // TODO: Not currently doing any extra configuration/validation for S3 here
            Command egressCmd = EgressHelper.CreateEgressCommand<S3StorageEgressProvider, S3StorageEgressProviderOptions>();

            rootCommand.Add(egressCmd);

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
