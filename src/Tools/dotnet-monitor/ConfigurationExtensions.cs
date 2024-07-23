// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if !NET7_0_OR_GREATER
using System.Reflection;
#endif

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ConfigurationExtensions
    {
        public static bool TryGetProvider(this IConfigurationBuilder builder, string key, [NotNullWhen(true)] out IConfigurationProvider? provider)
        {
            foreach (IConfigurationSource source in builder.Sources.Reverse())
            {
                if (source is ChainedConfigurationSource chainedSource &&
                    null != chainedSource.Configuration &&
                    chainedSource.Configuration.TryGetProviderAndValue(key, out provider, out _))
                {
                    return true;
                }
            }

            provider = null;
            return false;
        }

        public static bool TryGetProviderAndValue(this IConfiguration configuration, string key, [NotNullWhen(true)] out IConfigurationProvider? provider, out string? value)
        {
            if (configuration is IConfigurationRoot configurationRoot)
            {
                return configurationRoot.TryGetProviderAndValue(key, out provider, out value);
            }

            provider = null;
            value = null;
            return false;
        }

        public static bool TryGetProviderAndValue(this IConfigurationRoot configurationRoot, string key, [NotNullWhen(true)] out IConfigurationProvider? provider, out string? value)
        {
            foreach (IConfigurationProvider candidate in configurationRoot.Providers.Reverse())
            {
                if (candidate is ChainedConfigurationProvider chainedProvider &&
                    chainedProvider.TryGetInnerConfiguration(out IConfiguration innerConfiguration) &&
                    innerConfiguration.TryGetProviderAndValue(key, out provider, out value))
                {
                    return true;
                }

                if (candidate.TryGet(key, out value))
                {
                    provider = candidate;
                    return true;
                }
            }

            provider = null;
            value = null;
            return false;
        }

        private static bool TryGetInnerConfiguration(this ChainedConfigurationProvider provider, out IConfiguration configuration)
        {
#if NET7_0_OR_GREATER
            configuration = provider.Configuration;
            return true;
#else
            FieldInfo configField = provider.GetType().GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
            if (null != configField)
            {
                configuration = configField.GetValue(provider) as IConfiguration;
                return null != configuration;
            }

            configuration = null;
            return false;
#endif
        }
    }
}
