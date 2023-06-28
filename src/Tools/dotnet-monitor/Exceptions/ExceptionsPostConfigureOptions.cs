// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Stacks
{
    internal sealed class ExceptionsPostConfigureOptions : IPostConfigureOptions<ExceptionsOptions>
    {
        private readonly IConfiguration _configuration;

        public ExceptionsPostConfigureOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        void IPostConfigureOptions<ExceptionsOptions>.PostConfigure(string name, ExceptionsOptions options)
        {
            InProcessFeatureOptionsBinder.BindEnabled(
                options,
                ConfigurationKeys.InProcessFeatures_Exceptions,
                _configuration,
                ExceptionsOptionsDefaults.Enabled);
        }
    }
}
