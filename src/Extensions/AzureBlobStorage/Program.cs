// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            ILogger logger = Utilities.CreateLogger<AzureBlobEgressProviderOptions>();

            AzureBlobEgressProvider provider = new(logger);

            Action<ExtensionEgressPayload, AzureBlobEgressProviderOptions> configureOptions = (configPayload, options) =>
            {
                // If account key was not provided but the name was provided,
                // lookup the account key property value from EgressOptions.Properties
                if (string.IsNullOrEmpty(options.AccountKey) && !string.IsNullOrEmpty(options.AccountKeyName))
                {
                    if (configPayload.Properties.TryGetValue(options.AccountKeyName, out string accountKey))
                    {
                        options.AccountKey = accountKey;
                    }
                    else
                    {
                        logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.AccountKeyName);
                    }
                }

                // If shared access signature (SAS) was not provided but the name was provided,
                // lookup the SAS property value from EgressOptions.Properties
                if (string.IsNullOrEmpty(options.SharedAccessSignature) && !string.IsNullOrEmpty(options.SharedAccessSignatureName))
                {
                    if (configPayload.Properties.TryGetValue(options.SharedAccessSignatureName, out string sasSig))
                    {
                        options.SharedAccessSignature = sasSig;
                    }
                    else
                    {
                        logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.SharedAccessSignatureName);
                    }
                }

                // If queue shared access signature (SAS) was not provided but the name was provided,
                // lookup the SAS property value from EgressOptions.Properties
                if (string.IsNullOrEmpty(options.QueueSharedAccessSignature) && !string.IsNullOrEmpty(options.QueueSharedAccessSignatureName))
                {
                    if (configPayload.Properties.TryGetValue(options.QueueSharedAccessSignatureName, out string signature))
                    {
                        options.QueueSharedAccessSignature = signature;
                    }
                    else
                    {
                        logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.QueueSharedAccessSignatureName);
                    }
                }
            };

            return await SharedEntrypoint<AzureBlobEgressProviderOptions>.Entrypoint(args, provider, configureOptions);
        }
    }
}
