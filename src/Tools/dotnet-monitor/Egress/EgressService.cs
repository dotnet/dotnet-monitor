// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ILogger<EgressService> _logger;
        private readonly IEnumerable<IEgressProviderConfigurationProvider> _providers;
        private readonly IDictionary<string, IEgressProviderConfigurationProvider> _egressProviderMap;
        private readonly IDictionary<string, string> _providerNameToTypeMap;
        private readonly IServiceProvider _serviceProvider;

        public EgressService(
            IServiceProvider serviceProvider,
            ILogger<EgressService> logger,
            IConfiguration configuration,
            IEnumerable<IEgressProviderConfigurationProvider> providers)
        {
            _logger = logger;
            _providers = providers;
            _egressProviderMap = new ConcurrentDictionary<string, IEgressProviderConfigurationProvider>(StringComparer.OrdinalIgnoreCase);
            _providerNameToTypeMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _serviceProvider = serviceProvider;

            _changeRegistration = ChangeToken.OnChange(
                () => configuration.GetEgressSection().GetReloadToken(),
                Reload);

            Reload();
        }

        public void Dispose()
        {
            _changeRegistration.Dispose();
        }

        public void ValidateProvider(string providerName)
        {
            // GetProvider should never return null so no need to check; it will throw
            // if the egress provider could not be located or instantiated.
            string providerType = GetProviderType(providerName);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            string providerType = GetProviderType(providerName);
            IEgressProviderInternal provider = GetProvider(providerType);

            string value = await provider.EgressAsync(
                providerType,
                providerName,
                action,
                await CreateSettings(source, fileName, contentType, collectionRuleMetadata, token),
                token);

            return new EgressResult(value);
        }

        private string GetProviderType(string providerName)
        {
            if (!_providerNameToTypeMap.TryGetValue(providerName, out string providerType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }
            return providerType;
        }

        private IEgressProviderInternal GetProvider(string providerType)
        {
            if (!_egressProviderMap.TryGetValue(providerType, out IEgressProviderConfigurationProvider configProvider))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderTypeNotRegistered, providerType));
            }

            // Get the egress provider that matches the options type and return the weaker-typed
            // interface in order to allow egressing from the service without having to use reflection
            // to invoke it.            
            Type serviceType = typeof(IEgressProviderInternal<>).MakeGenericType(configProvider.OptionsType);
            object serviceReference = _serviceProvider.GetRequiredService(serviceType);
            IEgressProviderInternal typedServiceReference = (IEgressProviderInternal)serviceReference;
            return typedServiceReference;
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
            _egressProviderMap.Clear();
            _providerNameToTypeMap.Clear();

            // Deliberately fill the maps in the reverse order of how they are accessed
            // by the GetProvider method to avoid indeterminate accessing of the option
            // information.
            foreach (IEgressProviderConfigurationProvider provider in _providers)
            {
                foreach (string providerType in provider.ProviderTypes)
                {
                    _egressProviderMap.Add(providerType, provider);
                    IConfigurationSection typeSection = provider.GetProviderTypeConfigurationSection(providerType);

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
}
