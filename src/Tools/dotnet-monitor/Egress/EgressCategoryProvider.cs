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
    internal sealed class EgressCategoryProvider : IEgressCategoryProvider
    {
        public EgressCategoryProvider(IConfiguration configuration)
        {
            Configuration = configuration.GetEgressSection();
        }

        /// <inheritdoc/>
        public IConfiguration Configuration { get; }
    }
}
