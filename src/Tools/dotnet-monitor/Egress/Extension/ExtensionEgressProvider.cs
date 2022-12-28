// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private readonly IConfiguration _configuration;

        public ExtensionEgressProvider(IEgressPropertiesProvider propertyProvider, ExtensionDiscoverer extensionDiscoverer, ILogger<ExtensionEgressProvider> logger, IEgressProviderConfigurationProvider configurationProvider, IServiceProvider serviceProvider)
            : base(logger)
        {
            _propertyProvider = propertyProvider;
            _extensionDiscoverer = extensionDiscoverer;
            _configurationProvider = configurationProvider;
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
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
                Properties = _propertyProvider.GetAllProperties(),
                ProviderName = providerName,
                Configuration = GetConfigurationSection(providerName, providerType)
            };

            IEgressExtension ext = _extensionDiscoverer.FindExtension<IEgressExtension>(providerType);
            EgressArtifactResult result = await ext.EgressArtifact(payload, action, token);

            if (!result.Succeeded)
            {
                throw new EgressException(Strings.ErrorMessage_EgressExtensionFailed);
            }

            return result.ArtifactPath;
        }

        private string GetConfigurationSection(string providerName, string providerType)
        {
            try
            {
                IConfigurationSection providerTypeSection = _configurationProvider.GetConfigurationSection(providerType);
                IConfigurationSection providerNameSection = providerTypeSection.GetSection(providerName);

                var configAsDict = providerNameSection.AsEnumerable().ToDictionary(c => c.Key.Replace($"{ConfigurationKeys.Egress}:{providerType}:{providerName}:", string.Empty), c => c.Value);
                var json = JsonSerializer.Serialize(configAsDict);

                return json; // Could this return as empty instead of throwing an exception?
            }
            catch (Exception)
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }
        }
    }
}
