// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
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
            IEgressProviderConfigurationProvider configProvider = GetConfigProvider(providerName);
            IEgressProviderInternal _ = GetProvider(configProvider);
        }

        public string GetProviderCategory(string providerName)
        {
            IEgressProviderConfigurationProvider configProvider = GetConfigProvider(providerName);
            return configProvider.ProviderCategory;
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<CancellationToken, Task<Stream>> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            IEgressProviderConfigurationProvider configProvider = GetConfigProvider(providerName);
            IEgressProviderInternal provider = GetProvider(configProvider);

            string value = await provider.EgressAsync(
                configProvider.ProviderCategory,
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult(value);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            IEgressProviderConfigurationProvider configProvider = GetConfigProvider(providerName);
            IEgressProviderInternal provider = GetProvider(configProvider);

            string value = await provider.EgressAsync(
                configProvider.ProviderCategory,
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult(value);
        }

        private IEgressProviderConfigurationProvider GetConfigProvider(string providerName)
        {
            if (!_providerNameToTypeMap.TryGetValue(providerName, out string providerCategoryName))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }

            if (!_egressProviderMap.TryGetValue(providerCategoryName, out IEgressProviderConfigurationProvider configProvider) || configProvider.ProviderCategory != providerCategoryName)
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderTypeNotRegistered, providerCategoryName));
            }

            return configProvider;
        }

        private IEgressProviderInternal GetProvider(IEgressProviderConfigurationProvider configProvider)
        {
            // Get the egress provider that matches the options type and return the weaker-typed
            // interface in order to allow egressing from the service without having to use reflection
            // to invoke it.
            Type serviceType = typeof(IEgressProviderInternal<>).MakeGenericType(configProvider.OptionsType);
            object serviceReference = _serviceProvider.GetRequiredService(serviceType);
            IEgressProviderInternal typedServiceReference = (IEgressProviderInternal)serviceReference;
            return typedServiceReference;
        }

        private static EgressArtifactSettings CreateSettings(IEndpointInfo source, string fileName, string contentType)
        {
            EgressArtifactSettings settings = new();
            settings.Name = fileName;
            settings.ContentType = contentType;

            // Activity metadata
            Activity activity = Activity.Current;
            if (null != activity)
            {
                settings.Metadata.Add(
                    ActivityMetadataNames.ParentId,
                    activity.GetParentId());
                settings.Metadata.Add(
                    ActivityMetadataNames.SpanId,
                    activity.GetSpanId());
                settings.Metadata.Add(
                    ActivityMetadataNames.TraceId,
                    activity.GetTraceId());
            }

            // Artifact metadata
            settings.Metadata.Add(
                ArtifactMetadataNames.ArtifactSource.ProcessId,
                source.ProcessId.ToString(CultureInfo.InvariantCulture));
            settings.Metadata.Add(
                ArtifactMetadataNames.ArtifactSource.RuntimeInstanceCookie,
                source.RuntimeInstanceCookie.ToString("N"));

            return settings;
        }

        private void Reload()
        {
            _egressProviderMap.Clear();

            // Deliberately fill the maps in the reverse order of how they are accessed
            // by the GetProvider method to avoid indeterminate accessing of the option
            // information.
            foreach (IEgressProviderConfigurationProvider provider in _providers)
            {
                _egressProviderMap.Add(provider.ProviderCategory, provider);

                foreach (IConfigurationSection optionsSection in provider.Configuration.GetChildren())
                {
                    string providerName = optionsSection.Key;

                    if (_providerNameToTypeMap.TryGetValue(providerName, out string existingProviderCategory))
                    {
                        _logger.DuplicateEgressProviderIgnored(providerName, provider.ProviderCategory, existingProviderCategory);
                    }
                    else
                    {
                        _providerNameToTypeMap.Add(providerName, provider.ProviderCategory);
                    }
                }
            }
        }
    }
}
