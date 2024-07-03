// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal sealed class EgressProviderSource : IDisposable
    {
        private readonly Lazy<IDisposable> _changeRegistrationLazy;
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly ExtensionDiscoverer _extensionDiscoverer;
        private readonly ILogger _logger;
        private readonly IDictionary<string, string> _providerNameToTypeMap;
        public IReadOnlyCollection<string> ProviderNames { get; set; } = new List<string>();
        public event EventHandler? ProvidersChanged;

        public EgressProviderSource(
            IEgressConfigurationProvider configurationProvider,
            ExtensionDiscoverer extensionDiscoverer,
            ILogger<EgressProviderSource> logger)
        {
            _changeRegistrationLazy = new Lazy<IDisposable>(CreateChangeRegistration, LazyThreadSafetyMode.ExecutionAndPublication);
            _configurationProvider = configurationProvider;
            _extensionDiscoverer = extensionDiscoverer;
            _logger = logger;
            _providerNameToTypeMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_changeRegistrationLazy.IsValueCreated)
            {
                _changeRegistrationLazy.Value.Dispose();
            }
        }

        public void Initialize()
        {
            _ = _changeRegistrationLazy.Value;
        }

        public IEgressExtension GetEgressProvider(string name)
        {
            if (!_providerNameToTypeMap.TryGetValue(name, out string? providerType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, name));
            }

            return _extensionDiscoverer.FindExtension<IEgressExtension>(providerType);
        }

        private IDisposable CreateChangeRegistration()
        {
            IDisposable changeRegistration = ChangeToken.OnChange(
                _configurationProvider.GetReloadToken,
                Reload);

            Reload();

            return changeRegistration;
        }

        private void Reload()
        {
            _providerNameToTypeMap.Clear();

            foreach (string providerType in _configurationProvider.ProviderTypes)
            {
                try
                {
                    IEgressExtension _ = _extensionDiscoverer.FindExtension<IEgressExtension>(providerType);
                }
                catch (ExtensionException)
                {
                    _logger.EgressProviderTypeNotExist(providerType);
                }

                IConfigurationSection typeSection = _configurationProvider.GetProviderTypeConfigurationSection(providerType);

                foreach (IConfigurationSection optionsSection in typeSection.GetChildren())
                {
                    string providerName = optionsSection.Key;
                    if (_providerNameToTypeMap.TryGetValue(providerName, out string? existingProviderType))
                    {
                        _logger.DuplicateEgressProviderIgnored(providerName, providerType, existingProviderType);
                    }
                    else
                    {
                        _providerNameToTypeMap.Add(providerName, providerType);
                    }
                }
            }

            ProviderNames = new List<string>(_providerNameToTypeMap.Keys).AsReadOnly();

            ProvidersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
