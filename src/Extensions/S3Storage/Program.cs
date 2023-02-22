// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            ILogger logger = Utilities.CreateLogger<S3StorageEgressProviderOptions>();

            S3StorageEgressProvider provider = new(logger);

            Action<ExtensionEgressPayload, S3StorageEgressProviderOptions> configureOptions = (configPayload, options) =>
            {
                // TODO: Not currently doing any extra configuration/validation for S3 here
            };

            return await SharedEntrypoint<S3StorageEgressProviderOptions>.Entrypoint(args, provider, configureOptions);
        }
    }
}
