﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
        private readonly IDynamicOptionsSource<TOptions> _source;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;
        private readonly IValidateOptions<TOptions> _validation;

        public EgressProviderInternal(
            ILogger<EgressProviderInternal<TOptions>> logger,
            IEgressProvider<TOptions> provider,
            IDynamicOptionsSource<TOptions> source,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
            IValidateOptions<TOptions> validation)
        {
            _logger = logger;
            _provider = provider;
            _source = source;
            _postConfigures = postConfigures;
            _validation = validation;
        }

        /// <inheritdoc/>
        public Task<string> EgressAsync(
            string providerName,
            Func<CancellationToken, Task<Stream>> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            return _provider.EgressAsync(
                GetOptions(providerName),
                action,
                artifactSettings,
                token);
        }

        /// <inheritdoc/>
        public Task<string> EgressAsync(
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            return _provider.EgressAsync(
                GetOptions(providerName),
                action,
                artifactSettings,
                token);
        }

        private TOptions GetOptions(string providerName)
        {
            try
            {
                return _source.Get(providerName);
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
