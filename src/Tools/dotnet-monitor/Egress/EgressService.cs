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
        private readonly IDictionary<string, string> _providerTypeMap;
        private readonly IDictionary<string, Type> _optionsTypeMap;
        private readonly IServiceProvider _serviceProvider;

        public EgressService(
            IServiceProvider serviceProvider,
            ILogger<EgressService> logger,
            IConfiguration configuration,
            IEnumerable<IEgressProviderConfigurationProvider> providers)
        {
            _logger = logger;
            _providers = providers;
            _providerTypeMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _optionsTypeMap = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
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

        public async Task<EgressResult> EgressAsync(string providerName, Func<CancellationToken, Task<Stream>> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            string value = await GetProvider(providerName).EgressAsync(
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult(value);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            string value = await GetProvider(providerName).EgressAsync(
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult(value);
        }

        private IEgressProviderInternal GetProvider(string providerName)
        {
            if (!_providerTypeMap.TryGetValue(providerName, out string providerType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }

            if (!_optionsTypeMap.TryGetValue(providerType, out Type optionsType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderTypeNotRegistered, providerName));
            }

            // Get the egress provider that matches the options type and return the weaker-typed
            // interface in order to allow egressing from the service without having to use reflection
            // to invoke it.
            return (IEgressProviderInternal)_serviceProvider.GetRequiredService(
                typeof(IEgressProviderInternal<>).MakeGenericType(optionsType));
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
            _providerTypeMap.Clear();
            _optionsTypeMap.Clear();

            // Deliberately fill the maps in the reverse order of how they are accessed
            // by the GetProvider method to avoid indeterminate accessing of the option
            // information.
            foreach (IEgressProviderConfigurationProvider provider in _providers)
            {
                _optionsTypeMap.Add(provider.ProviderType, provider.OptionsType);

                foreach (IConfigurationSection optionsSection in provider.Configuration.GetChildren())
                {
                    string providerName = optionsSection.Key;
                    if (_providerTypeMap.TryGetValue(providerName, out string existingProviderType))
                    {
                        _logger.DuplicateEgressProviderIgnored(providerName, provider.ProviderType, existingProviderType);
                    }
                    else
                    {
                        _providerTypeMap.Add(providerName, provider.ProviderType);
                    }
                }
            }
        }
    }
}
