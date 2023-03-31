// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress service implementation required by the REST server.
    /// </summary>
    internal class EgressService : IEgressService, IDisposable
    {
        private readonly IDisposable _changeRegistration;
        private readonly ExtensionDiscoverer _extensionDiscoverer;
        private readonly ILogger<EgressService> _logger;
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly IDictionary<string, string> _providerNameToTypeMap;

        public EgressService(
            IEgressConfigurationProvider configurationProvider,
            ExtensionDiscoverer extensionDiscoverer,
            ILogger<EgressService> logger)
        {
            _configurationProvider = configurationProvider;
            _extensionDiscoverer = extensionDiscoverer;
            _logger = logger;
            _providerNameToTypeMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _changeRegistration = ChangeToken.OnChange(
                _configurationProvider.GetReloadToken,
                Reload);

            Reload();
        }

        public void Dispose()
        {
            _changeRegistration.Dispose();
        }

        public void ValidateProvider(string providerName)
        {
            // GetProviderType should never return null so no need to check; it will throw
            // if the egress provider could not be located or instantiated.
            string _ = GetProviderType(providerName);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            string providerType = GetProviderType(providerName);

            IEgressExtension extension = _extensionDiscoverer.FindExtension<IEgressExtension>(providerType);

            EgressArtifactResult result = await extension.EgressArtifact(
                providerName,
                await CreateSettings(source, fileName, contentType, collectionRuleMetadata, token),
                action,
                token);

            if (!result.Succeeded)
            {
                throw new EgressException(Strings.ErrorMessage_EgressExtensionFailed);
            }

            return new EgressResult(result.ArtifactPath);
        }

        private string GetProviderType(string providerName)
        {
            if (!_providerNameToTypeMap.TryGetValue(providerName, out string providerType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }
            return providerType;
        }

        private static async Task<EgressArtifactSettings> CreateSettings(IEndpointInfo source, string fileName, string contentType, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            EgressArtifactSettings settings = new();
            settings.Name = fileName;
            settings.ContentType = contentType;

            if (source.TargetFrameworkSupportsProcessEnv())
            {
                DiagnosticsClient client = new DiagnosticsClient(source.Endpoint);
                settings.EnvBlock = await client.GetProcessEnvironmentAsync(token);
            }

            // Activity metadata
            Activity activity = Activity.Current;
            if (null != activity)
            {
                AddMetadata(settings, ActivityMetadataNames.ParentId, activity.GetParentId());
                AddMetadata(settings, ActivityMetadataNames.SpanId, activity.GetSpanId());
                AddMetadata(settings, ActivityMetadataNames.TraceId, activity.GetTraceId());
            }

            if (null != collectionRuleMetadata)
            {
                AddMetadata(settings, CollectionRuleMetadataNames.CollectionRuleName, collectionRuleMetadata.CollectionRuleName);
                AddMetadata(settings, CollectionRuleMetadataNames.ActionListIndex, collectionRuleMetadata.ActionListIndex.ToString("D", CultureInfo.InvariantCulture));
                AddMetadata(settings, CollectionRuleMetadataNames.ActionName, collectionRuleMetadata.ActionName);
            }

            // Artifact metadata
            AddMetadata(settings, ArtifactMetadataNames.ArtifactSource.ProcessId, source.ProcessId.ToString(CultureInfo.InvariantCulture));
            AddMetadata(settings, ArtifactMetadataNames.ArtifactSource.RuntimeInstanceCookie, source.RuntimeInstanceCookie.ToString("N"));

            return settings;
        }

        private static void AddMetadata(EgressArtifactSettings settings, string key, string value)
        {
            settings.Metadata.Add($"{ToolIdentifiers.StandardPrefix}{key}", value);
        }

        private void Reload()
        {
            _providerNameToTypeMap.Clear();

            foreach (string providerType in _configurationProvider.ProviderTypes)
            {
                IConfigurationSection typeSection = _configurationProvider.GetProviderTypeConfigurationSection(providerType);

                foreach (IConfigurationSection optionsSection in typeSection.GetChildren())
                {
                    string providerName = optionsSection.Key;
                    if (_providerNameToTypeMap.TryGetValue(providerName, out string existingProviderType))
                    {
                        _logger.DuplicateEgressProviderIgnored(providerName, providerType, existingProviderType);
                    }
                    else
                    {
                        _providerNameToTypeMap.Add(providerName, providerType);
                    }
                }
            }
        }
    }
}
