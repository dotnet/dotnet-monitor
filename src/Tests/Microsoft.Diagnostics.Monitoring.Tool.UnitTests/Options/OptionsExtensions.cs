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

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
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
            MapObject(options, "DotNetMonitor_", variables);
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
            MapObject(options, string.Empty, variables);
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

        private static void MapDictionary(IDictionary dictionary, string prefix, IDictionary<string, string> map)
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
                        map);
                }
            }
        }

        private static void MapList(IList list, string prefix, IDictionary<string, string> map)
        {
            for (int index = 0; index < list.Count; index++)
            {
                object value = list[index];
                if (null != value)
                {
                    MapValue(
                        value,
                        FormattableString.Invariant($"{prefix}{index}"),
                        map);
                }
            }
        }

        private static void MapObject(object obj, string prefix, IDictionary<string, string> map)
        {
            foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.GetIndexParameters().Any())
                {
                    if (property.GetCustomAttribute<System.Text.Json.Serialization.JsonExtensionDataAttribute>() != null)
                    {
                        IDictionary<string,string> extendedProperties = (IDictionary<string,string>)property.GetValue(obj);
                        foreach(KeyValuePair<string,string> kvp in extendedProperties)
                        {
                            MapValue(kvp.Value, FormattableString.Invariant($"{prefix}{kvp.Key}"), map);
                        }
                    }
                    else
                    {
                        string propertyName = property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>()?.PropertyName ?? property.Name;

                        MapValue(
                            property.GetValue(obj),
                            FormattableString.Invariant($"{prefix}{propertyName}"),
                            map);
                    }
                }
            }
        }

        private static void MapValue(object value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                Type valueType = value.GetType();
                if (valueType.IsPrimitive || typeof(string) == valueType)
                {
                    map.Add(
                        valueName,
                        Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                else
                {
                    string prefix = FormattableString.Invariant($"{valueName}__");
                    if (value is IDictionary dictionary)
                    {
                        MapDictionary(dictionary, prefix, map);
                    }
                    else if (value is IList list)
                    {
                        MapList(list, prefix, map);
                    }
                    else
                    {
                        MapObject(value, prefix, map);
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
