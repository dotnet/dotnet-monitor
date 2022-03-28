// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Configure an <see cref="ExtensionEgressPayload"/> by binding to
    /// its associated Egress:{ProviderType}:{Name} section in the configuration.
    /// </summary>
    /// <remarks>
    /// Only named options are support for egress providers. This also binds the object specially by binding to the internal data structure.
    /// </remarks>
    internal sealed class EgressConfigureNamedOptions :
        IConfigureNamedOptions<EgressCategoryOptions>
    {
        private readonly IEgressCategoryProvider _provider;

        public EgressConfigureNamedOptions(IEgressCategoryProvider provider)
        {
            _provider = provider;
        }

        public void Configure(string name, EgressCategoryOptions options)
        {
            IConfigurationSection section = _provider.Configuration.GetSection(name);
            Debug.Assert(section.Exists());
            if (section.Exists())
            {
                options.ConfigurationSection = section;
            }
        }

        public void Configure(EgressCategoryOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
