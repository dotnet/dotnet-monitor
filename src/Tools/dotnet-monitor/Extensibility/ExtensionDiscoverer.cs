// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDiscoverer : IExtensionDiscoverer
    {
        private readonly IOrderedEnumerable<IExtensionRepository> _extensionRepos;
        private readonly ILogger<IExtensionDiscoverer> _logger;

        public ExtensionDiscoverer(IEnumerable<IExtensionRepository> extensionRepos, ILogger<IExtensionDiscoverer> logger)
        {
            _extensionRepos = extensionRepos.OrderBy(eRepo => eRepo.ResolvePriority);
            _logger = logger;
        }

        /// <inheritdoc/>
        public TExtensionType FindExtension<TExtensionType>(string extensionMoniker) where TExtensionType : class, IExtension
        {
            _logger.ExtensionProbeStart(extensionMoniker);
            foreach (IExtensionRepository repo in _extensionRepos)
            {
                _logger.ExtensionProbeRepo(extensionMoniker, repo);
                TExtensionType result = repo.FindExtension<TExtensionType>(extensionMoniker);
                if (result != null)
                {
                    _logger.ExtensionProbeSucceeded(extensionMoniker, result);
                    return result;
                }
            }
            _logger.ExtensionProbeFailed(extensionMoniker);
            return null;
        }
    }
}
