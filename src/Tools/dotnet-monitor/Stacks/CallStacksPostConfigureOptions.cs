// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Stacks
{
    internal sealed class CallStacksPostConfigureOptions : IPostConfigureOptions<CallStacksOptions>
    {
        private readonly IConfiguration _configuration;

        public CallStacksPostConfigureOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        void IPostConfigureOptions<CallStacksOptions>.PostConfigure(string name, CallStacksOptions options)
        {
            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                ConfigurationKeys.InProcessFeatures_CallStacks,
                _configuration,
                CallStacksOptionsDefaults.Enabled);
        }
    }
}
