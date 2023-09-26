// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    public sealed class EgressProperties
    {
        private readonly Dictionary<string, string> _properties;

        public EgressProperties(Dictionary<string, string> properties)
        {
            _properties = properties;
        }

        public bool TryGetValue(string key, out string value)
        {
            return _properties.TryGetValue(key, out value);
        }
    }
}
