// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Raises a notification that the <see cref="TOptions"/>
    /// may have changed when the Egress:Properties section changes.
    /// </summary>
    /// <remarks>
    /// Because the egress options are named but not registered at service configuration time,
    /// the standard <see cref="IOptionsChangeTokenSource{TOptions}"/> registration will not
    /// correctly notify when the named options change. The egress option provider names are
    /// not known at service configuration time, thus cannot create an <see cref="IOptionsChangeTokenSource{TOptions}"/>
    /// for each of the named options.
    /// 
    /// Instead, implement <see cref="IDynamicOptionsChangeTokenSource{TOptions}"/> in order
    /// to notify when any of the named options of type <typeparamref name="TOptions"/> have
    /// changed due to changes in the Egress:Properties configuration section.
    /// </remarks>
    internal sealed class EgressPropertiesConfigurationChangeTokenSource<TOptions> :
        ConfigurationDynamicChangeTokenSource<TOptions>
    {
        public EgressPropertiesConfigurationChangeTokenSource(IEgressPropertiesConfigurationProvider configuration)
            : base(configuration.Configuration)
        {
        }
    }
}
