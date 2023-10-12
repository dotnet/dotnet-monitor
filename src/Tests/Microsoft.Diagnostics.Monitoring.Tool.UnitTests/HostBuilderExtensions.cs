// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                ReplaceSource(builder.Sources, IsAspNetChainedConfigurationSource, values);
            });

            static bool IsAspNetChainedConfigurationSource(IConfigurationSource source)
            {
                // ASP.NET injects a precreated configuration source via AddConfiguration during host
                // configuration. This shows up as a chained configuration source when enumerating the
                // sources. Use a heuristic to find the chained configuration source added by ASP.NET.
                return source is ChainedConfigurationSource chainedSource &&
                    chainedSource.Configuration is ConfigurationRoot chainedConfiguration &&
                    chainedConfiguration.Providers.Any(p => IsEnvironmentConfigurationProvider(p, "ASPNETCORE_"));
            }

            static bool IsEnvironmentConfigurationProvider(IConfigurationProvider provider, string prefix)
            {
                if (!(provider is EnvironmentVariablesConfigurationProvider envProvider))
                {
                    return false;
                }

                FieldInfo prefixField = typeof(EnvironmentVariablesConfigurationProvider)
                    .GetField("_prefix", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(prefixField);

                return string.Equals(prefixField.GetValue(envProvider) as string, prefix, StringComparison.OrdinalIgnoreCase);
            }
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

            Assert.Fail($"Unable to find configuration source.");
        }
    }
}
