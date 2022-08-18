// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Raises a notification that the <see cref="TOptions"/>
    /// may have changed when the Egress:{ProviderType} section changes.
    /// </summary>
    internal sealed class EgressProviderConfigurationChangeTokenSource<TOptions> :
        ConfigurationChangeTokenSource<TOptions>
    {
        public EgressProviderConfigurationChangeTokenSource(IEgressProviderConfigurationProvider<TOptions> provider)
            : base(provider.GetTokenChangeSourceSection())
        {
        }
    }
}
