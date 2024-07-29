// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.TestCommon;
#endif
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class CommonOptionsExtensions
    {
        private const string KeySegmentSeparator = "__";

        /// <summary>
        /// Generates a map of options that can be passed directly to configuration via an in-memory collection.
        /// </summary>
        /// <remarks>
        /// Each key is the configuration path; each value is the configuration path value.
        /// </remarks>
        public static IDictionary<string, string> ToConfigurationValues(this RootOptions options)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapObject(options, string.Empty, ConfigurationPath.KeyDelimiter, variables);
            return variables;
        }

        /// <summary>
        /// Generates an environment variable map of the options.
        /// </summary>
        /// <remarks>
        /// Each key is the variable name; each value is the variable value.
        /// </remarks>
        public static IDictionary<string, string> ToEnvironmentConfiguration(this RootOptions options, bool useDotnetMonitorPrefix = false)
        {
            Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);
            MapObject(options, useDotnetMonitorPrefix ? ToolIdentifiers.StandardPrefix : string.Empty, KeySegmentSeparator, variables);
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
            MapObject(options, string.Empty, KeySegmentSeparator, variables);
            return variables;
        }

        private static void MapDictionary(IDictionary dictionary, string prefix, string separator, IDictionary<string, string> map)
        {
            foreach (var key in dictionary.Keys)
            {
                object? value = dictionary[key];

                if (null != value)
                {
                    string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
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
                object? value = list[index];
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

        private static void MapValue(object? value, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != value)
            {
                Type valueType = value.GetType();
                if (valueType.IsPrimitive ||
                    valueType.IsEnum ||
                    typeof(Guid) == valueType ||
                    typeof(string) == valueType ||
                    typeof(TimeSpan) == valueType)
                {
                    map.Add(
                        valueName,
                        ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
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
