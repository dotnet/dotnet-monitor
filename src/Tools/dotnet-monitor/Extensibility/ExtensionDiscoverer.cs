// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDiscoverer
    {
        private readonly ExtensionRepository[] _extensionRepos;
        private readonly ILogger<ExtensionDiscoverer> _logger;

        public ExtensionDiscoverer(IEnumerable<ExtensionRepository> extensionRepos, ILogger<ExtensionDiscoverer> logger)
        {
            _extensionRepos = extensionRepos.ToArray();
            _logger = logger;
        }

        /// <summary>
        /// Attempts to locate an extension with the given moniker and return it in the provided type.
        /// </summary>
        /// <typeparam name="TExtensionType">The type of the extension that must be found.</typeparam>
        /// <param name="extensionName">The string moniker used to refer to the extension</param>
        /// <returns></returns>
        /// <exception cref="ExtensionException">Thrown when the target extension is not found.</exception>
        public TExtensionType FindExtension<TExtensionType>(string extensionName) where TExtensionType : class, IExtension
        {
            _logger.ExtensionProbeStart(extensionName);
            foreach (ExtensionRepository repo in _extensionRepos)
            {
                bool found = repo.TryFindExtension(extensionName, out IExtension genericResult);
                if (found)
                {
                    bool isOfType = genericResult is TExtensionType;
                    if (isOfType)
                    {
                        _logger.ExtensionProbeSucceeded(extensionName, genericResult);
                        return (TExtensionType)genericResult;
                    }
                    else
                    {
                        _logger.ExtensionNotOfType(extensionName, genericResult, typeof(TExtensionType));
                    }
                }
            }
            _logger.ExtensionProbeFailed(extensionName);
            ExtensionException.ThrowNotFound(extensionName);

            // This will never get hit because the statement above should always throw
            return null;
        }
    }
}
