// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Stacks
{
    internal sealed class ExceptionsPostConfigureOptions :
        InProcessFeaturePostConfigureOptions<ExceptionsOptions>
    {
        public ExceptionsPostConfigureOptions(IConfiguration configuration)
            : base(configuration, ConfigurationKeys.InProcessFeatures_Exceptions, ExceptionsOptionsDefaults.Enabled)
        {
        }
    }
}
