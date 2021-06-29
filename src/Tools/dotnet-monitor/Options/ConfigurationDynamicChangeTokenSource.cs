using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Options
{
    internal class ConfigurationDynamicChangeTokenSource<TOptions> :
        IDynamicOptionsChangeTokenSource<TOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigurationDynamicChangeTokenSource(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IChangeToken GetChangeToken()
        {
            return _configuration.GetReloadToken();
        }
    }
}
