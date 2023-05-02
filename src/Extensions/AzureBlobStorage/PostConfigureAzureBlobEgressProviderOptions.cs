// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal sealed class PostConfigureAzureBlobEgressProviderOptions :
        IPostConfigureOptions<AzureBlobEgressProviderOptions>
    {
        private readonly ILogger _logger;
        private readonly EgressProperties _properties;

        public PostConfigureAzureBlobEgressProviderOptions(ILogger<AzureBlobEgressProvider> logger, EgressProperties properties)
        {
            _properties = properties;
            _logger = logger;
        }

        public void PostConfigure(string name, AzureBlobEgressProviderOptions options)
        {
            // If account key was not provided but the name was provided,
            // lookup the account key property value from the egress properties
            if (string.IsNullOrEmpty(options.AccountKey) && !string.IsNullOrEmpty(options.AccountKeyName))
            {
                if (_properties.TryGetValue(options.AccountKeyName, out string accountKey))
                {
                    options.AccountKey = accountKey;
                }
                else
                {
                    _logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.AccountKeyName);
                }
            }

            // If shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from the egress properties
            if (string.IsNullOrEmpty(options.SharedAccessSignature) && !string.IsNullOrEmpty(options.SharedAccessSignatureName))
            {
                if (_properties.TryGetValue(options.SharedAccessSignatureName, out string sasSig))
                {
                    options.SharedAccessSignature = sasSig;
                }
                else
                {
                    _logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.SharedAccessSignatureName);
                }
            }

            // If queue shared access signature (SAS) was not provided but the name was provided,
            // lookup the SAS property value from the egress properties
            if (string.IsNullOrEmpty(options.QueueSharedAccessSignature) && !string.IsNullOrEmpty(options.QueueSharedAccessSignatureName))
            {
                if (_properties.TryGetValue(options.QueueSharedAccessSignatureName, out string signature))
                {
                    options.QueueSharedAccessSignature = signature;
                }
                else
                {
                    _logger.EgressProviderUnableToFindPropertyKey(Constants.AzureBlobStorageProviderName, options.QueueSharedAccessSignatureName);
                }
            }
        }
    }
}
