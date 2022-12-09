// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// A configuration source that allows blocking the reading of the Urls option
    /// from configuration providers that are configured at a lower priority.
    /// </summary>
    internal sealed class ServerUrlsBlockingConfigurationSource :
        IConfigurationSource
    {
        private readonly ServerUrlsBlockingConfigurationManager _manager;

        public ServerUrlsBlockingConfigurationSource(ServerUrlsBlockingConfigurationManager manager)
        {
            _manager = manager;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ServerUrlsBlockingConfigurationProvider(_manager);
        }
    }
}
