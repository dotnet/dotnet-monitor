// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage.AzureBlob
{
    /// <summary>
    /// Fills AccountKey and SharedAccessSignature from Egress:Properties if they do not have values.
    /// </summary>
    internal sealed class AzureBlobEgressPostConfigureOptions :
        IPostConfigureOptions<AzureBlobEgressProviderOptions>
    {
        private readonly IEgressPropertiesProvider _provider;
        private ILogger Logger { get; }


        public AzureBlobEgressPostConfigureOptions(
            IEgressPropertiesProvider provider,
            ILogger logger)
        {
            _provider = provider;
            Logger = logger;
        }

        public void PostConfigure(string name, AzureBlobEgressProviderOptions options)
        {
            // If account key was not provided but the name was provided,
            // lookup the account key property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.AccountKey) &&
                !string.IsNullOrEmpty(options.AccountKeyName))
            {
                if (_provider.TryGetPropertyValue(options.AccountKeyName, out string key))
                {
                    options.AccountKey = key;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(name, options.AccountKeyName);
                }
            }

            // If shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.SharedAccessSignature) &&
                !string.IsNullOrEmpty(options.SharedAccessSignatureName))
            {
                if (_provider.TryGetPropertyValue(options.SharedAccessSignatureName, out string signature))
                {
                    options.SharedAccessSignature = signature;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(name, options.SharedAccessSignatureName);
                }
            }

            // If queue shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from EgressOptions.Properties
            if (string.IsNullOrEmpty(options.QueueSharedAccessSignature) &&
                !string.IsNullOrEmpty(options.QueueSharedAccessSignatureName))
            {
                if (_provider.TryGetPropertyValue(options.QueueSharedAccessSignatureName, out string signature))
                {
                    options.QueueSharedAccessSignature = signature;
                }
                else
                {
                    Logger.EgressProviderUnableToFindPropertyKey(name, options.QueueSharedAccessSignatureName);
                }
            }
        }
    }
}
