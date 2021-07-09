// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob
{
    /// <summary>
    /// Fills AccountKey and SharedAccessSignature from Egress:Properties if they do not have values.
    /// </summary>
    internal sealed class AzureBlobEgressPostConfigureOptions :
        IPostConfigureOptions<AzureBlobEgressProviderOptions>
    {
        private readonly ILogger<AzureBlobEgressPostConfigureOptions> _logger;
        private readonly IEgressPropertiesProvider _provider;

        public AzureBlobEgressPostConfigureOptions(
            ILogger<AzureBlobEgressPostConfigureOptions> logger,
            IEgressPropertiesProvider provider)
        {
            _logger = logger;
            _provider = provider;
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
                    _logger.EgressProviderUnableToFindPropertyKey(name, options.AccountKeyName);
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
                    _logger.EgressProviderUnableToFindPropertyKey(name, options.SharedAccessSignatureName);
                }
            }
        }
    }
}
