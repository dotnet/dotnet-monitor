// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class HostBuilderExtensions
    {
        /// <summary>
        /// Replaces the ASPNETCORE_* environment configuration source with an in-memory configuration source.
        /// </summary>
        public static IHostBuilder ReplaceAspnetEnvironment(this IHostBuilder builder, IDictionary<string, string> values = null)
        {
            return builder.ConfigureHostConfiguration(builder =>
            {
                ReplaceEnvironment(builder.Sources, prefix: "ASPNETCORE_", values);
            });
        }

        /// <summary>
        /// Replaces the DOTNET_* environment configuration source with an in-memory configuration source.
        /// </summary>
        public static IHostBuilder ReplaceDotnetEnvironment(this IHostBuilder builder, IDictionary<string, string> values = null)
        {
            return builder.ConfigureHostConfiguration(builder =>
            {
                ReplaceEnvironment(builder.Sources, prefix: "DOTNET_", values);
            });
        }

        /// <summary>
        /// Replaces the DOTNETMONITOR_* environment configuration source with an in-memory configuration source.
        /// </summary>
        public static IHostBuilder ReplaceMonitorEnvironment(this IHostBuilder builder, IDictionary<string, string> values = null)
        {
            return builder.ConfigureAppConfiguration(builder =>
            {
                ReplaceEnvironment(builder.Sources, ToolIdentifiers.StandardPrefix, values);
            });
        }

        /// <summary>
        /// Replaces the environment configuration source with the given prefix with an in-memory configuration source.
        /// </summary>
        private static void ReplaceEnvironment(IList<IConfigurationSource> sources, string prefix, IDictionary<string, string> values)
        {
            ReplaceSource(sources, s => IsEnvironmentConfigurationSource(s, prefix), values);

            static bool IsEnvironmentConfigurationSource(IConfigurationSource source, string prefix)
            {
                return source is EnvironmentVariablesConfigurationSource envSource &&
                    string.Equals(envSource.Prefix, prefix, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Replaces configuration sources with an in-memory configuration source when
        /// the original source matches the specified filter.
        /// </summary>
        private static void ReplaceSource(IList<IConfigurationSource> sources, Func<IConfigurationSource, bool> filter, IDictionary<string, string> values)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (filter(sources[i]))
                {
                    MemoryConfigurationSource source = new();
                    source.InitialData = values;
                    sources[i] = source;
                    return;
                }
            }

            Assert.False(true, $"Unable to find configuration source.");
        }
    }
}
