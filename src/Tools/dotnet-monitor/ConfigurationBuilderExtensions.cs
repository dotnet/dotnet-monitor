// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder ConfigureStorageDefaults(this IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.Storage, nameof(StorageOptions.DumpTempFolder)), StorageOptionsDefaults.DumpTempFolder }
            });
        }
    }
}
