// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class InProcessFeaturePostConfigureOptions<T> :
        IPostConfigureOptions<T>
        where T : class, IInProcessFeatureOptions
    {
        private readonly IConfiguration _configuration;
        private readonly string _configurationKey;
        private readonly bool _enabledByDefault;

        public InProcessFeaturePostConfigureOptions(IConfiguration configuration, string configurationKey, bool enabledByDefault)
        {
            _configuration = configuration;
            _configurationKey = configurationKey;
            _enabledByDefault = enabledByDefault;
        }

        void IPostConfigureOptions<T>.PostConfigure(string? name, T options)
        {
            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                _configurationKey,
                _configuration,
                _enabledByDefault);
        }
    }
}
