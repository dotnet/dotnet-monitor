// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class ExtensionEgressProvider :
        EgressProvider<ExtensionEgressProviderOptions>
    {
        private readonly IEgressPropertiesProvider _propertyProvider;
        private readonly ExtensionDiscoverer _extensionDiscoverer;
        private readonly IEgressProviderConfigurationProvider _configurationProvider;

        public ExtensionEgressProvider(IEgressPropertiesProvider propertyProvider, ExtensionDiscoverer extensionDiscoverer, ILogger<ExtensionEgressProvider> logger, IEgressProviderConfigurationProvider configurationProvider)
            : base(logger)
        {
            _propertyProvider = propertyProvider;
            _extensionDiscoverer = extensionDiscoverer;
            _configurationProvider = configurationProvider;
        }

        public override async Task<string> EgressAsync(
            string providerType,
            string providerName,
            ExtensionEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            ExtensionEgressPayload payload = new ExtensionEgressPayload()
            {
                Settings = artifactSettings,
                Configuration = GetConfigurationSection(providerName, providerType),
                Properties = _propertyProvider.GetAllProperties(),
                ProviderName = providerName,
            };

            IEgressExtension ext = _extensionDiscoverer.FindExtension<IEgressExtension>(providerType);
            EgressArtifactResult result = await ext.EgressArtifact(payload, action, token);

            if (!result.Succeeded)
            {
                throw new EgressException(Strings.ErrorMessage_EgressExtensionFailed);
            }

            return result.ArtifactPath;
        }

        private Dictionary<string, string> GetConfigurationSection(string providerName, string providerType)
        {
            IConfigurationSection providerTypeSection = _configurationProvider.GetConfigurationSection(providerType);
            IConfigurationSection providerNameSection = providerTypeSection.GetSection(providerName);

            if (!providerNameSection.Exists())
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }

            var configAsDict = new Dictionary<string, string>();

            foreach (var kvp in providerNameSection.AsEnumerable(makePathsRelative: true))
            {
                // Only exclude null values that have children.
                if (kvp.Value == null)
                {
                    if (providerNameSection.GetSection(kvp.Key).GetChildren().Any())
                    {
                        continue;
                    }
                }

                configAsDict[kvp.Key] = kvp.Value;
            }

            return configAsDict;
        }
    }
}
