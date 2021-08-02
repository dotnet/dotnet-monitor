// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

#if !UNITTEST
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
#endif

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static class OptionsExtensions
    {
        /// <summary>
        /// Generates an environment variable map of the options.
        /// </summary>
        /// <remarks>
        /// Each key is the variable name; each value is the variable value.
        /// </remarks>
        public static IDictionary<string, string> ToEnvironmentConfiguration(this RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapObject(options, "DotNetMonitor_", "__", variables);
            return variables;
        }

        public static IDictionary<string, string> ToConfigurationValues(this RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapObject(options, string.Empty, ":", variables);
            return variables;
        }

        /// <summary>
        /// Generates a key-per-file map of the options.
        /// </summary>
        /// <remarks>
        /// Each key is the file name; each value is the file content.
        /// </remarks>
        public static IDictionary<string, string> ToKeyPerFileConfiguration(this RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapObject(options, string.Empty, "__", variables);
            return variables;
        }

        /// <summary>
        /// Sets API Key authentication.
        /// </summary>
        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, byte[] apiKey)
        {
            if (null == options.ApiAuthentication)
            {
                options.ApiAuthentication = new ApiAuthenticationOptions();
            }

            using var hashAlgorithm = HashAlgorithm.Create(algorithmName);

            byte[] hash = hashAlgorithm.ComputeHash(apiKey);
            options.ApiAuthentication.ApiKeyHash = ToHexString(hash);
            options.ApiAuthentication.ApiKeyHashType = algorithmName;

            return options;
        }

        public static RootOptions AddFileSystemEgress(this RootOptions options, string name, string outputPath)
        {
            var egressProvider = new FileSystemEgressProviderOptions()
            {
                DirectoryPath = outputPath
            };

            options.Egress = new EgressOptions
            {
                FileSystem = new Dictionary<string, FileSystemEgressProviderOptions>()
                {
                    { name, egressProvider }
                }
            };

            return options;
        }

        public static CollectionRuleOptions CreateCollectionRule(this RootOptions rootOptions, string name)
        {
            CollectionRuleOptions options = new();
            rootOptions.CollectionRules.Add(name, options);
            return options;
        }

        private static void MapDictionary(IDictionary dictionary, string prefix, string separator, IDictionary<string, string> map)
        {
            foreach (var key in dictionary.Keys)
            {
                object value = dictionary[key];
                if (null != value)
                {
                    string keyString = Convert.ToString(key, CultureInfo.InvariantCulture);
                    MapValue(
                        value,
                        FormattableString.Invariant($"{prefix}{keyString}"),
                        separator,
                        map);
                }
            }
        }

        private static void MapList(IList list, string prefix, string separator, IDictionary<string, string> map)
        {
            for (int index = 0; index < list.Count; index++)
            {
                object value = list[index];
                if (null != value)
                {
                    MapValue(
                        value,
                        FormattableString.Invariant($"{prefix}{index}"),
                        separator,
                        map);
                }
            }
        }

        private static void MapObject(object obj, string prefix, string separator, IDictionary<string, string> map)
        {
            foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.GetIndexParameters().Any())
                {
                    MapValue(
                        property.GetValue(obj),
                        FormattableString.Invariant($"{prefix}{property.Name}"),
                        separator,
                        map);
                }
            }
        }

        private static void MapValue(object value, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != value)
            {
                Type valueType = value.GetType();
                if (valueType.IsPrimitive ||
                    valueType.IsEnum ||
                    typeof(string) == valueType ||
                    typeof(TimeSpan) == valueType)
                {
                    map.Add(
                        valueName,
                        Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                else
                {
                    string prefix = FormattableString.Invariant($"{valueName}{separator}");
                    if (value is IDictionary dictionary)
                    {
                        MapDictionary(dictionary, prefix, separator, map);
                    }
                    else if (value is IList list)
                    {
                        MapList(list, prefix, separator, map);
                    }
                    else
                    {
                        MapObject(value, prefix, separator, map);
                    }
                }
            }
        }

        private static string ToHexString(byte[] data)
        {
            StringBuilder builder = new(2 * data.Length);
            foreach (byte b in data)
            {
                builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }
    }
}
