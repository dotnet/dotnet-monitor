// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// A wrapper implementation that takes an egress provider name, gets the egress provider options
    /// based on that name, and invokes the corresponding <see cref="IEgressProvider{TOptions}"/> implementation
    /// to perform the egress action.
    /// </summary>
    internal class EgressProviderInternal<TOptions> :
        IEgressProviderInternal<TOptions>
        where TOptions : class
    {
        private readonly ILogger<EgressProviderInternal<TOptions>> _logger;
        private readonly IEgressProvider<TOptions> _provider;
        private readonly IOptionsMonitor<TOptions> _monitor;

        public EgressProviderInternal(
            ILogger<EgressProviderInternal<TOptions>> logger,
            IEgressProvider<TOptions> provider,
            IOptionsMonitor<TOptions> monitor)
        {
            _logger = logger;
            _provider = provider;
            _monitor = monitor;
        }

        /// <inheritdoc/>
        public Task<string> EgressAsync(
            string providerType,
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            return _provider.EgressAsync(
                providerType,
                providerName,
                GetOptions(providerName),
                action,
                artifactSettings,
                token);
        }

        private TOptions GetOptions(string providerName)
        {
            try
            {
                TOptions opts = _monitor.Get(providerName);
                return opts;
            }
            catch (OptionsValidationException ex)
            {
                foreach (string failure in ex.Failures)
                {
                    _logger.EgressProviderOptionsValidationFailure(providerName, failure);
                }

                _logger.EgressProviderInvalidOptions(providerName);

                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }
        }
    }
}
