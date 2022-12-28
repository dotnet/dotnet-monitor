// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal sealed class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public IDictionary<string, string> Properties { get; set; }
<<<<<<< HEAD
        public string Configuration { get; set; }
=======
        public IDictionary<string, string> Configuration { get; set; }
        public IConfigurationSection ConfigurationSection { get; set; }
>>>>>>> e21fe0fe2a5f15c2d65b34a7991dde7b0ca5ddf3
        public string ProviderName { get; set; }
    }
}
