// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.TestCommon;
#endif
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;

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
            MapRootOptions(options, string.Empty, ConfigurationPath.KeyDelimiter, variables);
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
            MapRootOptions(options, useDotnetMonitorPrefix ? ToolIdentifiers.StandardPrefix : string.Empty, KeySegmentSeparator, variables);
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
            MapRootOptions(options, string.Empty, KeySegmentSeparator, variables);
            return variables;
        }

        // private static void MapDictionary(IDictionary dictionary, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     foreach (var key in dictionary.Keys)
        //     {
        //         object? value = dictionary[key];

        //         if (null != value)
        //         {
        //             string keyString = ConvertUtils.ToString(key, CultureInfo.InvariantCulture);
        //             MapValue(
        //                 value,
        //                 FormattableString.Invariant($"{prefix}{keyString}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        // private static void MapList(IList list, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     for (int index = 0; index < list.Count; index++)
        //     {
        //         object? value = list[index];
        //         if (null != value)
        //         {
        //             MapValue(
        //                 value,
        //                 FormattableString.Invariant($"{prefix}{index}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        private static void MapRootOptions(RootOptions obj, string prefix, string separator, IDictionary<string, string> map)
        {
            // TODO: in Tests, it has an additional property. Weird.
            MapAuthenticationOptions(obj.Authentication, FormattableString.Invariant($"{prefix}{nameof(obj.Authentication)}"), separator, map);
            // GlobalCounterOptions
            MapGlobalCounterOptions(obj.GlobalCounter, FormattableString.Invariant($"{prefix}{nameof(obj.GlobalCounter)}"), separator, map);
            // InProcessFeaturesOptions
            // CorsConfigurationOptions
            // DiagnosticPortOptions
            // EgressOptions
            // MetricsOptions
            // StorageOptions
            // ProcessFilterOptions
            // CollectionRuleDefaultsOptions
            // Templates
            // DotnetMonitorDebugOptions
            // FOR TESTS: Logging?
        }

        // private static void MapObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(object obj, string prefix, string separator, IDictionary<string, string> map)
        // {
        //     foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        //     {
        //         if (!property.GetIndexParameters().Any())
        //         {
        //             MapValue(
        //                 property.GetValue(obj),
        //                 FormattableString.Invariant($"{prefix}{property.Name}"),
        //                 separator,
        //                 map);
        //         }
        //     }
        // }

        private static void MapAuthenticationOptions(AuthenticationOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapMonitorApiKeyOptions(obj.MonitorApiKey, prefix, separator, map);
                MapAzureAdOptions(obj.AzureAd, prefix, separator, map);
            }
        }

        private static void MapMonitorApiKeyOptions(MonitorApiKeyOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(obj.Subject, FormattableString.Invariant($"{prefix}{nameof(obj.Subject)}"), map);
                MapString(obj.PublicKey, FormattableString.Invariant($"{prefix}{nameof(obj.PublicKey)}"), map);
                MapString(obj.Issuer, FormattableString.Invariant($"{prefix}{nameof(obj.Issuer)}"), map);
            }
        }

        private static void MapString(string? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, value);
            }
        }

        private static void MapAzureAdOptions(AzureAdOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapUri(obj.Instance, FormattableString.Invariant($"{prefix}{nameof(obj.Instance)}"), separator, map);
                MapString(obj.TenantId, FormattableString.Invariant($"{prefix}{nameof(obj.TenantId)}"), map);
                MapString(obj.ClientId, FormattableString.Invariant($"{prefix}{nameof(obj.ClientId)}"), map);
                MapUri(obj.AppIdUri, FormattableString.Invariant($"{prefix}{nameof(obj.AppIdUri)}"), separator, map);
                MapString(obj.RequiredRole, FormattableString.Invariant($"{prefix}{nameof(obj.RequiredRole)}"), map);
            }
        }

        private static void MapUri(Uri? value, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != value)
            {
                // TODO!
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapString(value.AbsolutePath, FormattableString.Invariant($"{prefix}{nameof(value.AbsolutePath)}"), map);
                MapString(value.AbsoluteUri, FormattableString.Invariant($"{prefix}{nameof(value.AbsoluteUri)}"), map);
                MapString(value.Authority, FormattableString.Invariant($"{prefix}{nameof(value.Authority)}"), map);
                MapString(value.DnsSafeHost, FormattableString.Invariant($"{prefix}{nameof(value.DnsSafeHost)}"), map);
                MapString(value.Fragment, FormattableString.Invariant($"{prefix}{nameof(value.Fragment)}"), map);
                MapString(value.Host, FormattableString.Invariant($"{prefix}{nameof(value.Host)}"), map);
                // MapHostNameType()
                MapString(value.IdnHost, FormattableString.Invariant($"{prefix}{nameof(value.IdnHost)}"), map);
                // MapBool(value.IsAbsoluteUri, FormattableString.Invariant($"{prefix}{nameof(value.IsAbsoluteUri)}"), map);
                // MapBool(value.IsDefaultPort, FormattableString.Invariant($"{prefix}{nameof(value.IsDefaultPort)}"), map);
                // MapBool(value.IsFile, FormattableString.Invariant($"{prefix}{nameof(value.IsFile)}"), map);
                // MapBool(value.IsLoopback, FormattableString.Invariant($"{prefix}{nameof(value.IsLoopback)}"), map);
                // MapBool(value.IsUnc, FormattableString.Invariant($"{prefix}{nameof(value.IsUnc)}"), map);
                MapString(value.LocalPath, FormattableString.Invariant($"{prefix}{nameof(value.LocalPath)}"), map);
                MapString(value.OriginalString, FormattableString.Invariant($"{prefix}{nameof(value.OriginalString)}"), map);
                MapString(value.PathAndQuery, FormattableString.Invariant($"{prefix}{nameof(value.PathAndQuery)}"), map);
                // MapInt(value.Port, FormattableString.Invariant($"{prefix}{nameof(value.Port)}"), map);
                MapString(value.Query, FormattableString.Invariant($"{prefix}{nameof(value.Query)}"), map);
                MapString(value.Scheme, FormattableString.Invariant($"{prefix}{nameof(value.Scheme)}"), map);
                // MapStringArray(value.Segments, FormattableString.Invariant($"{prefix}{nameof(value.Segments)}"), separator, map);
                // MapBool(value.UserEscaped, FormattableString.Invariant($"{prefix}{nameof(value.UserEscaped)}"), map);
                MapString(value.UserInfo, FormattableString.Invariant($"{prefix}{nameof(value.UserInfo)}"), map);
            }
        }

        private static void MapGlobalCounterOptions(GlobalCounterOptions? obj, string valueName, string separator, IDictionary<string, string> map)
        {
            if (null != obj)
            {
                string prefix = FormattableString.Invariant($"{valueName}{separator}");
                MapFloat(obj.IntervalSeconds, FormattableString.Invariant($"{prefix}{nameof(obj.IntervalSeconds)}"), map);
                MapInt(obj.MaxHistograms, FormattableString.Invariant($"{prefix}{nameof(obj.MaxHistograms)}"), map);
                MapInt(obj.MaxTimeSeries, FormattableString.Invariant($"{prefix}{nameof(obj.MaxTimeSeries)}"), map);
                // MapDictionary(obj.Providers, prefix, separator, map);
            }
        }

        private static void MapFloat(float? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void MapInt(int? value, string valueName, IDictionary<string, string> map)
        {
            if (null != value)
            {
                map.Add(valueName, ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        // private static void MapValue(object? value, string valueName, string separator, IDictionary<string, string> map)
        // {
        //     if (null != value)
        //     {
        //         Type valueType = value.GetType();
        //         if (valueType.IsPrimitive ||
        //             valueType.IsEnum ||
        //             typeof(Guid) == valueType ||
        //             typeof(string) == valueType ||
        //             typeof(TimeSpan) == valueType)
        //         {
        //             map.Add(
        //                 valueName,
        //                 ConvertUtils.ToString(value, CultureInfo.InvariantCulture));
        //         }
        //         else
        //         {
        //             string prefix = FormattableString.Invariant($"{valueName}{separator}");
        //             if (value is IDictionary dictionary)
        //             {
        //                 MapDictionary(dictionary, prefix, separator, map);
        //             }
        //             else if (value is IList list)
        //             {
        //                 MapList(list, prefix, separator, map);
        //             }
        //             else
        //             {
        //                 MapObject(value, prefix, separator, map);
        //             }
        //         }
        //     }
        // }

        // private static string ToHexString(byte[] data)
        // {
        //     StringBuilder builder = new(2 * data.Length);
        //     foreach (byte b in data)
        //     {
        //         builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        //     }
        //     return builder.ToString();
        // }
    }
}
