// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal sealed class WellKnownExtensionRepository :
        ExtensionRepository
    {
        private readonly Dictionary<string, IWellKnownExtensionFactory> _factories;

        public WellKnownExtensionRepository(IEnumerable<IWellKnownExtensionFactory> factories)
        {
            _factories = factories.ToDictionary(f => f.Name, StringComparer.Ordinal);
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            if (!_factories.TryGetValue(extensionName, out IWellKnownExtensionFactory factory))
            {
                extension = null;
                return false;
            }

            extension = factory.Create();
            return true;
        }
    }
}
